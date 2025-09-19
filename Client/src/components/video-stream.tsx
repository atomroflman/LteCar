'use client';

import { useEffect, useRef, useState } from 'react';

interface VideoStreamProps {
  carId?: string;
  className?: string;
}

export default function VideoStream({ carId = "default", className }: VideoStreamProps) {
  const imgRef = useRef<HTMLImageElement>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isConnected, setIsConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [retryCount, setRetryCount] = useState(0);
  const maxRetries = 5;

  useEffect(() => {
    let mounted = true;
    
    const startVideoStream = () => {
      if (!mounted || !imgRef.current) return;
      
      const img = imgRef.current;
      
      // Event Handlers
      const handleLoad = () => {
        if (mounted) {
          setIsLoading(false);
          setIsConnected(true);
          setError(null);
          setRetryCount(0);
        }
      };
      
      const handleError = () => {
        if (!mounted) return;
        
        setIsConnected(false);
        setIsLoading(false);
        
        if (retryCount < maxRetries) {
          setError(`Connection lost, retrying... (${retryCount + 1}/${maxRetries})`);
          
          // Retry nach kurzer Wartezeit
          setTimeout(() => {
            if (mounted) {
              setRetryCount(prev => prev + 1);
              setIsLoading(true);
              img.src = buildStreamUrl();
            }
          }, 2000 + (retryCount * 1000)); // Exponential backoff
        } else {
          setError('Video stream unavailable. Please check connection.');
        }
      };
      
      // Event Listeners
      img.addEventListener('load', handleLoad);
      img.addEventListener('error', handleError);
      
      // Stream URL erstellen und starten
      const buildStreamUrl = () => {
        const baseUrl = '/api/videostream/video';
        const params = new URLSearchParams();
        
        if (carId && carId !== 'default') {
          params.append('carId', carId);
        }
        
        // Timestamp für Cache-Busting
        params.append('t', Date.now().toString());
        
        return `${baseUrl}?${params.toString()}`;
      };
      
      // Stream starten
      img.src = buildStreamUrl();
      
      // Cleanup function
      return () => {
        img.removeEventListener('load', handleLoad);
        img.removeEventListener('error', handleError);
      };
    };

    const cleanup = startVideoStream();
    
    return () => {
      mounted = false;
      cleanup?.();
    };
  }, [carId, retryCount]);

  const handleRetry = () => {
    setRetryCount(0);
    setIsLoading(true);
    setError(null);
    
    if (imgRef.current) {
      // Force reload durch neue URL
      const baseUrl = '/api/videostream/video';
      const params = new URLSearchParams();
      
      if (carId && carId !== 'default') {
        params.append('carId', carId);
      }
      
      params.append('t', Date.now().toString());
      imgRef.current.src = `${baseUrl}?${params.toString()}`;
    }
  };

  return (
    <div className={`video-stream-container relative ${className || ''}`}>
      {/* Status Indicator */}
      <div className="absolute top-2 left-2 z-10 flex items-center space-x-2">
        <div className={`w-3 h-3 rounded-full ${
          isConnected ? 'bg-green-500' : 
          isLoading ? 'bg-yellow-500 animate-pulse' : 
          'bg-red-500'
        }`}></div>
        <span className="text-white text-sm bg-black bg-opacity-50 px-2 py-1 rounded">
          {isConnected ? 'Live' : 
           isLoading ? 'Connecting...' : 
           'Disconnected'}
        </span>
      </div>

      {/* Loading Overlay */}
      {isLoading && (
        <div className="absolute inset-0 flex items-center justify-center bg-gray-900 bg-opacity-75 z-20">
          <div className="text-center text-white">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-white mx-auto mb-2"></div>
            <div>Connecting to video stream...</div>
          </div>
        </div>
      )}
      
      {/* Error Overlay */}
      {error && !isLoading && (
        <div className="absolute inset-0 flex items-center justify-center bg-red-900 bg-opacity-75 z-20">
          <div className="text-center text-white p-4">
            <div className="text-red-200 mb-4">{error}</div>
            <button 
              onClick={handleRetry}
              className="bg-red-600 hover:bg-red-700 text-white px-4 py-2 rounded transition-colors"
            >
              Retry Connection
            </button>
          </div>
        </div>
      )}
      
      {/* Video Stream */}
      <img
        ref={imgRef}
        alt="Live Camera Stream"
        className="w-full h-auto max-w-full"
        style={{ 
          display: (isLoading && !error) ? 'none' : 'block',
          minHeight: '240px',
          backgroundColor: '#1a1a1a'
        }}
        draggable={false}
      />
      
      {/* Stream Info */}
      {isConnected && (
        <div className="absolute bottom-2 right-2 z-10">
          <span className="text-white text-xs bg-black bg-opacity-50 px-2 py-1 rounded">
            MJPEG Stream {carId !== 'default' ? `(${carId})` : ''}
          </span>
        </div>
      )}
    </div>
  );
}
