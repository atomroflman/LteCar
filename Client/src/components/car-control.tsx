import React, { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";

type GamepadState = {
  id: string;
  axes: number[];
  buttons: number[];
};

interface CarState{
    id: string;
    driverId: string;
    driverName: string;
}

var uiHubConnection: signalR.HubConnection;
var controlHubConnection: signalR.HubConnection;

export default function GamepadViewer() {
  const [gamepad, setGamepad] = useState<GamepadState | null>(null);
  const [cars, setCars] = useState<CarState[] | null>(null);
  const [carKey, setCarKey] = useState<string>("");
  const [carId, setCarId] = useState<string>("");
  const [carSession, setCarSession] = useState<string>("");

  useEffect(() => {
    let animationFrame: number;

    if (uiHubConnection == null) {
        uiHubConnection = new signalR.HubConnectionBuilder()
            .withUrl("/carui")
            .withAutomaticReconnect()
            .build();
        uiHubConnection.on("CarStateUpdated", (newState: CarState) => {
            let tmpCars = cars?.filter(c => c.id != newState.id) ?? [];
            tmpCars.push(newState);
            tmpCars = tmpCars.sort((a,b) => a.driverName > b.driverName ? 1 : -1);
            if (tmpCars.length == 1)
                setCarId(tmpCars[0].id);
            setCars(tmpCars);
        })
        uiHubConnection.start().then(() => {
            uiHubConnection.invoke("UiClientConnected")
                .then((value: CarState[]) => {
                    console.log(value);
                    setCars(value);
                    if (value.length == 1)
                        setCarId(value[0].id);
                });
        });
    }
    if (controlHubConnection == null) {
        controlHubConnection = new signalR.HubConnectionBuilder()
            .withUrl("/control")
            .withAutomaticReconnect()
            .build();
        controlHubConnection.start();
    }

    const updateGamepad = () => {
      const gp = navigator.getGamepads()[0]; // Nimm den ersten Gamepad
      if (gp) {
        setGamepad({
          id: gp.id,
          axes: gp.axes.slice(),
          buttons: gp.buttons.map((b) => b.value),
        });
      }
      animationFrame = requestAnimationFrame(updateGamepad);
    };

    window.addEventListener("gamepadconnected", () => {
      console.log("🎮 Gamepad verbunden!");
      updateGamepad();
    });

    window.addEventListener("gamepaddisconnected", () => {
      console.log("🔌 Gamepad getrennt.");
      setGamepad(null);
      cancelAnimationFrame(animationFrame);
    });

    return () => cancelAnimationFrame(animationFrame);
  }, []);

  function aquireCarControl() {
    console.log("Aquiring Car: ", carId, carKey);
    if (!carId) return;
    if (!carKey) return;
    controlHubConnection.invoke("AquireCarControl", carId, carKey)
        .then((res: string) => setCarSession(res));
  }

  if (!gamepad) {
    return <div className="p-4">🎮 Bitte Gamepad anschließen…</div>;
  }

  return (
    <div className="p-4 space-y-4">
        <select onSelect={(e) => setCarId(e.currentTarget.value)}>
            {cars?.map(c => <option key={c.id} selected={c.id == carId}>{c.id}</option>)}
        </select>
        <label htmlFor="textInput" className="block mb-2 font-medium">
        Car Key:
      </label>
      <input
        type="text"
        id="carKeyInput"
        value={carKey}
        onChange={(e) => setCarKey(e.target.value)}
        className="p-2 border rounded w-full"
      />
      <button onClick={aquireCarControl}>Aquire Control</button>
      <h2 className="text-xl font-bold">🎮 {gamepad.id}</h2>
      <div>
        <h3 className="font-semibold">🕹️ Achsen:</h3>
        <ul className="list-disc ml-4">
          {gamepad.axes.map((value, i) => (
            <li key={i}>
              Achse {i}: <span className="font-mono">{value.toFixed(2)}</span>
            </li>
          ))}
        </ul>
      </div>

      <div>
        <h3 className="font-semibold">🔘 Buttons:</h3>
        <ul className="list-disc ml-4">
          {gamepad.buttons.map((value, i) => (
            <li key={i}>
              Button {i}:{" "}
              <span
                className={
                  value > 0
                    ? "text-green-600 font-bold"
                    : "text-gray-400 font-mono"
                }
              >
                {value.toFixed(2)}
              </span>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}