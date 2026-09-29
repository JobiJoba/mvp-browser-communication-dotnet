const CHANNEL_NAME = "case-side-panel-dnd-v1";

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

    dotNetRef.invokeMethodAsync("OnBrowserMessage", JSON.stringify(data));
  };

  return {
    getTabId: () => tabId,
    subscribe(ref) {
      dotNetRef = ref;
    },
    publish(messageJson) {
      const message = typeof messageJson === "string" ? JSON.parse(messageJson) : messageJson;
      channel.postMessage({ ...message, tabId });
    },
    dispose() {
      channel.close();
      dotNetRef = null;
    },
  };
}
