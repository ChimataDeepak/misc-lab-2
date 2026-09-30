import { createApp } from "./app.js";
const secret = process.env.MISCLAB_WEBHOOK_SECRET;
if (!secret) { console.error("MISCLAB_WEBHOOK_SECRET required"); process.exit(2); }
createApp({ secret }).listen(Number(process.env.PORT ?? 3000));
