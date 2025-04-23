declare module './janus.js' {
    interface JanusInitOptions {
        debug?: boolean | 'all';
        callback?: () => void;
    }

    interface JanusAttachOptions {
        plugin: string;
        success: (pluginHandle: any) => void;
        error: (error: Error) => void;
        onmessage?: (msg: any, jsep?: any) => void;
        onremotestream?: (stream: MediaStream) => void;
        oncleanup?: () => void;
    }

    interface JanusConstructorOptions {
        server: string;
        success: () => void;
        error: (error: Error) => void;
        destroyed?: () => void;
    }

    export default class Janus {
        constructor(options: JanusConstructorOptions);
        static init(options: JanusInitOptions): void;
        attach(options: JanusAttachOptions): void;
        destroy(): void;
    }
}