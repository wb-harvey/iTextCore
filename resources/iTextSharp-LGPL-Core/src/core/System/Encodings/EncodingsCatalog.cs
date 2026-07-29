using System.Text;

namespace iTextCore.System.Encodings
{
    public static class EncodingsCatalog
    {
        static EncodingsCatalog()
        {
#if !NET40 && !NET45
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        }

        public static Encoding GetEncoding(int codepage)
        {
            return Encoding.GetEncoding(codepage);
        }

        public static Encoding GetEncoding(string name)
        {
            return Encoding.GetEncoding(name);
        }
    }
}
