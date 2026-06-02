import React, { createContext, useContext, useEffect, useState, useCallback, useRef } from 'react';
import { HubConnectionBuilder, LogLevel, HttpTransportType } from '@microsoft/signalr';
import { getAccessTokenFromStorage } from '../utils/tokenUtils';

const SignalRContext = createContext(null);

export const SignalRProvider = ({ children }) => {
    const [connection, setConnection] = useState(null);
    const [isConnected, setIsConnected] = useState(false);
    const connectionRef = useRef(null);
    const isStartingRef = useRef(false);

    const startConnection = useCallback(async () => {
        if (connectionRef.current || isStartingRef.current) return;

        const token = getAccessTokenFromStorage();
        if (!token) return;

        isStartingRef.current = true;
        
        const newConnection = new HubConnectionBuilder()
            .withUrl("/gamehub", {
                accessTokenFactory: () => getAccessTokenFromStorage(),
                skipNegotiation: false,
                transport: HttpTransportType.WebSockets
            })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Information)
            .build();

        try {
            await newConnection.start();
            console.log("SignalR Connected.");
            connectionRef.current = newConnection;
            setConnection(newConnection);
            setIsConnected(true);
            isStartingRef.current = false;

            newConnection.onclose(() => {
                setIsConnected(false);
                connectionRef.current = null;
                setConnection(null);
                isStartingRef.current = false;
            });

            newConnection.onreconnecting(() => {
                setIsConnected(false);
            });

            newConnection.onreconnected(() => {
                setIsConnected(true);
            });

        } catch (err) {
            console.error("SignalR Connection Error: ", err);
            isStartingRef.current = false;
            // Повторная попытка через 5 сек
            setTimeout(startConnection, 5000);
        }
    }, []);

    const stopConnection = useCallback(async () => {
        if (connectionRef.current) {
            await connectionRef.current.stop();
            connectionRef.current = null;
            setConnection(null);
            setIsConnected(false);
            isStartingRef.current = false;
        }
    }, []);

    // Обертка для вызова методов хаба
    const joinRoom = useCallback(async (roomId) => {
        if (connectionRef.current && isConnected) {
            try {
                await connectionRef.current.invoke("JoinRoom", roomId.toString());
                console.log(`Joined SignalR group: ${roomId}`);
            } catch (err) {
                console.error("JoinRoom Error: ", err);
            }
        }
    }, [isConnected]);

    const leaveRoom = useCallback(async (roomId) => {
        if (connectionRef.current && isConnected) {
            try {
                await connectionRef.current.invoke("LeaveRoom", roomId.toString());
                console.log(`Left SignalR group: ${roomId}`);
            } catch (err) {
                console.error("LeaveRoom Error: ", err);
            }
        }
    }, [isConnected]);

    return (
        <SignalRContext.Provider value={{ 
            connection, 
            isConnected, 
            startConnection, 
            stopConnection,
            joinRoom,
            leaveRoom
        }}>
            {children}
        </SignalRContext.Provider>
    );
};

export const useSignalR = () => {
    const context = useContext(SignalRContext);
    if (!context) {
        throw new Error("useSignalR must be used within a SignalRProvider");
    }
    return context;
};
