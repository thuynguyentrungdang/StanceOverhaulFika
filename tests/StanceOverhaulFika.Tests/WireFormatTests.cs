using Fika.Core.Networking.LiteNetLib.Utils;
using StanceOverhaulFika.Core;
using StanceOverhaulFika.Net;
using Xunit;

namespace StanceOverhaulFika.Tests
{
    /// <summary>Serialises with Fika's real NetDataWriter/NetDataReader.</summary>
    public class WireFormatTests
    {
        private static StancePosePacket RoundTrip(StancePosePacket packet, out int size)
        {
            var writer = new NetDataWriter();
            packet.Serialize(writer);
            size = writer.Length;

            var reader = new NetDataReader();
            reader.SetSource(writer.Data, 0, writer.Length);
            var back = new StancePosePacket();
            back.Deserialize(reader);
            Assert.True(reader.EndOfData, "deserialize must consume exactly what serialize wrote");
            return back;
        }

        [Fact]
        public void Packet_round_trips_every_field()
        {
            var sent = new StancePosePacket
            {
                Seq = 41234,
                NetId = 7,
                Pose = new QPose(5, StanceFlags.Active | StanceFlags.TacSprint, -1234, 2345, -3456, 4567, -5678, 6789)
            };

            StancePosePacket got = RoundTrip(sent, out _);

            Assert.Equal(sent.Seq, got.Seq);
            Assert.Equal(sent.NetId, got.NetId);
            Assert.Equal(sent.Pose, got.Pose);
        }

        [Fact]
        public void Extreme_values_survive()
        {
            var sent = new StancePosePacket
            {
                Seq = ushort.MaxValue,
                NetId = int.MaxValue,
                Pose = new QPose(byte.MaxValue, byte.MaxValue, short.MinValue, short.MaxValue, 0, short.MinValue, short.MaxValue, 0)
            };

            Assert.Equal(sent.Pose, RoundTrip(sent, out _).Pose);
            Assert.Equal(int.MaxValue, RoundTrip(sent, out _).NetId);
        }

        [Fact]
        public void Payload_is_18_bytes()
        {
            RoundTrip(new StancePosePacket(), out int size);
            Assert.Equal(2 + 4 + 1 + 1 + 6 * 2, size);
        }

        [Fact]
        public void Two_packets_back_to_back_parse_independently()
        {
            var a = new StancePosePacket { Seq = 1, NetId = 2, Pose = new QPose(1, 2, 3, 4, 5, 6, 7, 8) };
            var b = new StancePosePacket { Seq = 9, NetId = 8, Pose = new QPose(7, 6, 5, 4, 3, 2, 1, 0) };

            var writer = new NetDataWriter();
            a.Serialize(writer);
            b.Serialize(writer);

            var reader = new NetDataReader();
            reader.SetSource(writer.Data, 0, writer.Length);
            var ra = new StancePosePacket(); ra.Deserialize(reader);
            var rb = new StancePosePacket(); rb.Deserialize(reader);

            Assert.Equal(a.Pose, ra.Pose);
            Assert.Equal(b.Pose, rb.Pose);
            Assert.Equal(9, rb.Seq);
            Assert.True(reader.EndOfData);
        }
    }
}
