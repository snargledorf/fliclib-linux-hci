using FliclibDotNetClient;

namespace FlicLibTests
{
    public class BdaddrTests
    {
        private static readonly BluetoothAddress bdaddr1 = new(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAA });

        [Test]
        public void Parse()
        {
            string s = "AA:90:78:56:34:12";

            var bdaddr = BluetoothAddress.Parse(s);

            Assert.That(bdaddr, Is.EqualTo(bdaddr1));
        }

        [Test]
        public void NotEquals()
        {
            var b = new BluetoothAddress(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAA });

            Assert.That(b, Is.Not.EqualTo(BluetoothAddress.Blank));
        }

        [Test]
        public void EqualOperator()
        {
            var b = new BluetoothAddress(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAA });

            Assert.That(b == bdaddr1, Is.True);
            Assert.That(b == BluetoothAddress.Blank, Is.False);
        }

        [Test]
        public void NotEqualOperator()
        {
            var b = new BluetoothAddress(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAA });

            Assert.That(b != bdaddr1, Is.False);
            Assert.That(b != BluetoothAddress.Blank, Is.True);
        }

        [Test]
        public void Hashcode()
        {
            var b = new BluetoothAddress(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAA });

            Assert.That(b.GetHashCode(), Is.EqualTo(bdaddr1.GetHashCode()));
            Assert.That(b.GetHashCode(), Is.Not.EqualTo(BluetoothAddress.Blank.GetHashCode()));
        }
    }
}