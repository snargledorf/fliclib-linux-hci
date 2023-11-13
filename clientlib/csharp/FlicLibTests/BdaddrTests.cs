using FliclibDotNetClient;

namespace FlicLibTests
{
    public class BdaddrTests
    {
        private static readonly Bdaddr bdaddr1 = new(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAA });

        [Test]
        public void Parse()
        {
            string s = "AA:90:78:56:34:12";

            var bdaddr = Bdaddr.Parse(s);

            Assert.That(bdaddr, Is.EqualTo(bdaddr1));
        }

        [Test]
        public void NotEquals()
        {
            var b = new Bdaddr(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAA });

            Assert.That(b, Is.Not.EqualTo(Bdaddr.Blank));
        }

        [Test]
        public void EqualOperator()
        {
            var b = new Bdaddr(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAA });

            Assert.That(b == bdaddr1, Is.True);
            Assert.That(b == Bdaddr.Blank, Is.False);
        }

        [Test]
        public void NotEqualOperator()
        {
            var b = new Bdaddr(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAA });

            Assert.That(b != bdaddr1, Is.False);
            Assert.That(b != Bdaddr.Blank, Is.True);
        }

        [Test]
        public void Hashcode()
        {
            var b = new Bdaddr(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAA });

            Assert.That(b.GetHashCode(), Is.EqualTo(bdaddr1.GetHashCode()));
            Assert.That(b.GetHashCode(), Is.Not.EqualTo(Bdaddr.Blank.GetHashCode()));
        }
    }
}