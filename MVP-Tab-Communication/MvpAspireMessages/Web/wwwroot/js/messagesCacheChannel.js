const CHANNEL_NAME = "mvp-aspire-messages-cache-v1";

export function createChannel() {
  const tabId = crypto.randomUUID();
  const channel = new BroadcastChannel(CHANNEL_NAME);
  let dotNetRef = null;

  channel.onmessage = (event) => {
    if (!dotNetRef) {
      return;
    }

    const data = event.data;
    if (!data || data.tabId === tabId) {
      return;
    }

    // Strip tabId so System.Text.Json can bind the envelope (Type + Message only).
    const { tabId: _ignored, ...payload } = data;
    dotNetRef.invokeMethodAsync("OnBrowserMessage", JSON.stringify(payload));
  };

  return {
    subscribe(ref) {
      dotNetRef = ref;
    },
    publish(messageJson) {
      const message = typeof messageJson === "string" ? JSON.parse(messageJson) : messageJson;
      channel.postMessage({ ...message, tabId });
    },
    closeTab() {
      window.close();
    },
    dispose() {
      channel.close();
      dotNetRef = null;
    },
  };
}
