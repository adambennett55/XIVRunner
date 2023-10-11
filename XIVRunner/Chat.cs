using FFXIVClientStructs.FFXIV.Client.System.Framework;
using System.Runtime.InteropServices;
using System.Text;

namespace XIVRunner;

internal static class Chat
{
    [StructLayout(LayoutKind.Explicit)]
    private readonly struct ChatPayload : IDisposable
    {
        [FieldOffset(0)]
        private readonly IntPtr textPtr;
        [FieldOffset(16)]
        private readonly ulong textLen;
        [FieldOffset(8)]
        private readonly ulong unk1;
        [FieldOffset(24)]
        private readonly ulong unk2;
        internal ChatPayload(byte[] stringBytes)
        {
            textPtr = Marshal.AllocHGlobal(stringBytes.Length + 30);
            Marshal.Copy(stringBytes, 0, textPtr, stringBytes.Length);
            Marshal.WriteByte(textPtr + stringBytes.Length, 0);
            textLen = (ulong)(stringBytes.Length + 1);
            unk1 = 64;
            unk2 = 0;
        }
        public void Dispose()
        {
            Marshal.FreeHGlobal(textPtr);
        }
    }

    private delegate void ProcessChatBoxDelegate(IntPtr uiModule, IntPtr message, IntPtr unused, byte a4);
    private static ProcessChatBoxDelegate? _processChatBox;

    public static void Init()
    {
        if (_processChatBox == null
            && (Service.SigScanner?.TryScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9", out var processChatBoxPtr) ?? false))
        {
            _processChatBox = Marshal.GetDelegateForFunctionPointer<ProcessChatBoxDelegate>(processChatBoxPtr);
        }
    }

    public static unsafe void SendMessage(string message)
    {
        if (_processChatBox == null) return;

        var bytes = Encoding.UTF8.GetBytes(message);
        var uiModule = (IntPtr)Framework.Instance()->GetUiModule();
        using var payload = new ChatPayload(bytes);
        var mem1 = Marshal.AllocHGlobal(400);
        Marshal.StructureToPtr(payload, mem1, false);
        _processChatBox(uiModule, mem1, IntPtr.Zero, 0);
        Marshal.FreeHGlobal(mem1);
    }
}
