using itextsharp.System.Encodings;
using Xunit;

namespace itextsharp.Tests.System.Encodings
{
    public class EncodingsCatalogTests
    {
        [Theory]
        [InlineData(932)]
        [InlineData(936)]
        [InlineData(1251)]
        public void GetEncodingByCodePage_returnsEncoding(int codepage)
        {
            var encoding = EncodingsCatalog.GetEncoding(codepage);
            Assert.Equal(codepage, encoding.CodePage);
        }

        [Theory]
        [InlineData("Windows-1252")]
        [InlineData("Shift-JIS")]
        [InlineData("GB2312")]
        public void GetEncodingByName_returnsEncoding(string name)
        {
            var encoding = EncodingsCatalog.GetEncoding(name);
            Assert.NotNull(encoding);
        }
    }
}
