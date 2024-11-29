using CTunnel.Core.Enums;
using CTunnel.Core.Model;
using Microsoft.AspNetCore.Http;

namespace CTunnel.Core.Expand
{
    public static class HttpContextExpand
    {
        public static TunnelModel? ReadParameter(this HttpContext httpContext)
        {
            var res = new TunnelModel();
            if (httpContext.Request.Headers.TryGetValue(nameof(TunnelModel.Id), out var Id))
                res.Id = Id!;
            else
                return null;
            //if (
            //    httpContext.Request.Headers.TryGetValue(
            //        nameof(TunnelModel.AuthCode),
            //        out var AuthCode
            //    )
            //)
            //    res.AuthCode = AuthCode!;
            //else
            //    return null;
            //if (
            //    httpContext.Request.Headers.TryGetValue(
            //        nameof(TunnelModel.DomainName),
            //        out var DomainName
            //    )
            //)
            //    res.DomainName = DomainName!;
            //else
            //    return null;
            if (httpContext.Request.Headers.TryGetValue(nameof(TunnelModel.Type), out var Type))
                res.Type = (TunnelTypeEnum)Enum.Parse(typeof(TunnelTypeEnum), Type!)!;
            else
                return null;
            if (
                httpContext.Request.Headers.TryGetValue(
                    nameof(TunnelModel.ListenPort),
                    out var ServerPort
                )
            )
                res.ListenPort = Convert.ToInt32(ServerPort);
            else
                return null;
            return res;
        }
    }
}
