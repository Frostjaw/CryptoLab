namespace CryptoLab.NetworkModule
{
    using System.Net;
    using System.Collections.Generic;

    public class DNS
    {
        private List<IPEndPoint> m_IPEndPoints = new List<IPEndPoint>();

        public DNS(int nodeId, List<NodeInfo> nodeInfos)
        {
            foreach(var nodeInfo in nodeInfos)
            {
                if (nodeInfo.Id == nodeId)
                {
                    continue;
                }

                m_IPEndPoints.Add(new IPEndPoint(IPAddress.Loopback, nodeInfo.Port));
            }
        }

        public List<IPEndPoint> GetListIPEndPoints(IPEndPoint self_IPEndPoint)
        {
            return m_IPEndPoints;
        }
    }
}
