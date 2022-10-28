
using Vuplex.WebView;

namespace UnipassWallet
{
    // Inherit from EventArgs<string> for backwards compatibility.
    public class KeyboardInputEventArgs : EventArgs<string>
    {

        public KeyboardInputEventArgs(string value, KeyModifier modifiers) : base(value)
        {

            Modifiers = modifiers;
        }

        public readonly KeyModifier Modifiers;
    }
}
