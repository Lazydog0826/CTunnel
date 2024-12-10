using System.Net.Sockets;
using CTunnel.Share.Enums;

namespace CTunnel.Share.Expand
{
    public static class TunnelTypeEnumExtend
    {
        /// <summary>
        /// 将TunnelTypeEnum类型转成对应的ProtocolType
        /// </summary>
        /// <param name="tunnelTypeEnum"></param>
        /// <returns></returns>
        public static ProtocolType ToProtocolType(this TunnelTypeEnum tunnelTypeEnum)
        {
            return tunnelTypeEnum switch
            {
                TunnelTypeEnum.Udp => ProtocolType.Udp,
                _ => ProtocolType.Tcp,
            };
        }
    }
}
