using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace AgendaDashboard.Utilities;

internal class GlobalKeybind : IDisposable
{
    /// <summary>
    /// Event that is raised when the global keyboard shortcut is pressed.
    /// </summary>
    public event EventHandler? Pressed;

    private readonly int _id;
    private readonly HwndSource _source;

    /// <param name="window">
    /// A window object that will be used to get the HwndSource to register the hotkey hook on. The HwndSource receives WM_HOTKEY messages.
    /// </param>
    /// <param name="key"></param>
    /// <param name="modifiers"></param>
    /// <param name="id">The internal id to use for the keybind.</param>
    internal GlobalKeybind(Window window, Key key, ModifierKeys modifiers, int id)
    {
        _id = id;
        _source = (PresentationSource.FromVisual(window) as HwndSource)!;
        _source.AddHook(HwndHook);
        // TODO: Throw an exception if registration fails
        RegisterHotKey(_source.Handle, _id, (uint)modifiers, (uint)KeyInterop.VirtualKeyFromKey(key));
    }

    public void Dispose()
    {
        UnregisterHotKey(_source.Handle, _id);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int wmHotkey = 0x0312;
        if (msg != wmHotkey || wParam.ToInt32() != _id) return IntPtr.Zero; // TODO: Is this the right return value?

        Pressed?.Invoke(this, EventArgs.Empty);
        handled = true; // Mark the message as handled
        return IntPtr.Zero; // TODO: Is this the right return value?
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}