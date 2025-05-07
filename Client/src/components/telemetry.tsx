import React, { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";

interface TelemetryData {
    name: string;
    value: string;
}

const Telemetry: React.FC = () => {
    const [telemetry, setTelemetry] = useState<TelemetryData[]>([]);
    const [isConnected, setIsConnected] = useState(false);

    useEffect(() => {
        // Create a connection to the SignalR hub
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/telemetry")
            .withAutomaticReconnect()
            .build();

        // Start the connection and handle telemetry updates
        connection
            .start()
            .then(() => {
                console.log("Connected to SignalR hub");
                setIsConnected(true);

                connection.on("UpdateTelemetry", (valueName: string, value: string) => {
                    setTelemetry((prev) => {
                        // Update the telemetry data, replacing existing values by name
                        const updated = [...prev];
                        const index = updated.findIndex((item) => item.name === valueName);
                        if (index !== -1) {
                            updated[index].value = value;
                        } else {
                            updated.push({ name: valueName, value });
                        }
                        return updated;
                    });
                });
            })
            .catch((err) => {
                console.error("Error connecting to SignalR hub:", err);
                setIsConnected(false);
            });

        // Handle disconnection
        connection.onclose(() => {
            console.warn("Disconnected from SignalR hub");
            setIsConnected(false);
        });

        // Cleanup on component unmount
        return () => {
            connection.stop().catch((err) => console.error("Error disconnecting:", err));
        };
    }, []);

    return (
        <div>
            {!isConnected && (
                <div className="text-red-500 font-bold p-4">
                    Not connected to the telemetry hub.
                </div>
            )}
            <div className="grid grid-cols-2 gap-4 p-4">
                {telemetry.map((item, index) => (
                    <div
                        key={index}
                        className="border p-2 rounded shadow-sm bg-gray-100 flex justify-between"
                    >
                        <span className="font-bold">{item.name}</span>
                        <span>{item.value}</span>
                    </div>
                ))}
            </div>
        </div>
    );
};

export default Telemetry;