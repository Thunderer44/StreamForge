// signaling-server.js
import { WebSocketServer } from "ws";

const wss = new WebSocketServer({ port: 8080, host: '0.0.0.0' });

wss.on("connection", (ws) => {
  console.log("New client connected", ws._socket.remoteAddress);

  ws.on("message", (message) => {
    // Broadcast frames to all other clients
    wss.clients.forEach((client) => {
      if (client !== ws && client.readyState === 1) {
        client.send(message);
      }
    });
  });

  ws.on("close", () => console.log("Client disconnected"));
});

console.log("✅ Streaming server running on ws://0.0.0.0:8080");
console.log("📱 Access from other devices using your local IP");
