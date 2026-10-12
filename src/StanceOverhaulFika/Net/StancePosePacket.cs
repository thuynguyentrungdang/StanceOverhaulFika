using Fika.Core.Networking.LiteNetLib.Utils;
using StanceOverhaulFika.Core;

namespace StanceOverhaulFika.Net
{
    /// <summary>
    /// One player's stance pose. 18 bytes + Fika header.
    /// Fika identifies packets by a hash of the full type name: if this layout ever changes,
    /// rename the type (e.g. StancePosePacketV2) so mismatched peers fail loudly instead of misparsing.
    /// </summary>
    public struct StancePosePacket : INetSerializable
    {
        public ushort Seq;
        public int NetId;
        public QPose Pose;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Seq);
            writer.Put(NetId);
            writer.Put(Pose.Stance);
            writer.Put(Pose.Flags);
            writer.Put(Pose.PX); writer.Put(Pose.PY); writer.Put(Pose.PZ);
            writer.Put(Pose.RX); writer.Put(Pose.RY); writer.Put(Pose.RZ);
        }

        public void Deserialize(NetDataReader reader)
        {
            Seq = reader.GetUShort();
            NetId = reader.GetInt();
            byte stance = reader.GetByte();
            byte flags = reader.GetByte();
            short px = reader.GetShort(), py = reader.GetShort(), pz = reader.GetShort();
            short rx = reader.GetShort(), ry = reader.GetShort(), rz = reader.GetShort();
            Pose = new QPose(stance, flags, px, py, pz, rx, ry, rz);
        }
    }
}
