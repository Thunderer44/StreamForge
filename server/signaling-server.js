// signaling-server.js - WebRTC Signaling Server
import { WebSocketServer } from "ws";

const wss = new WebSocketServer({ port: 8080, host: '0.0.0.0' });

wss.on("connection", (ws) => {
  console.log("✅ New peer connected", ws._socket.remoteAddress);
  console.log("   Total clients:", wss.clients.size);

  ws.on("message", (message) => {
    try {
      const data = JSON.parse(message.toString());
      console.log("📨 Received:", data.type, "from", ws._socket.remoteAddress);
      
      let sent = 0;
      // Broadcast signaling messages to all other peers
      wss.clients.forEach((client) => {
        console.log("   Client readyState:", client.readyState, "(1=OPEN)");
        if (client !== ws && client.readyState === 1) {
          client.send(message);
          sent++;
          console.log("   ✅ Sent to peer");
        }
      });
      console.log("   Broadcasted to", sent, "peer(s)");
    } catch (e) {
      console.error("❌ Invalid message:", e);
    }
  });

  ws.on("close", () => {
    console.log("❌ Peer disconnected");
    console.log("   Total clients:", wss.clients.size);
  });
});

console.log("✅ WebRTC signaling server running on ws://0.0.0.0:8080");
console.log("📱 Access from other devices using your local IP");
