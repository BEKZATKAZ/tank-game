using Newtonsoft.Json;
using System.Text;
using Tanks.Core;
using Tanks.Shared.Bootstrap;

namespace Tanks.Server.Bootstrap
{
    public interface IClientConnectionValidator
    {
        ResultType<ClientConnectionPayload> Validate(byte[] data);
    }

    public sealed class ClientConnectionValidator : IClientConnectionValidator
    {
        public ResultType<ClientConnectionPayload> Validate(byte[] data)
        {
            ClientConnectionPayload payload;

            try
            {
                string json = Encoding.UTF8.GetString(data);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return Result.Failure<ClientConnectionPayload>("missing-payload");
                }

                payload = JsonConvert.DeserializeObject<ClientConnectionPayload>(json);
            }
            catch
            {
                return Result.Failure<ClientConnectionPayload>("invalid-payload");
            }

            if (string.IsNullOrWhiteSpace(payload.DisplayName))
            {
                return Result.Failure<ClientConnectionPayload>("missing-username");
            }

            if (payload.DisplayName.Length > 25)
            {
                return Result.Failure<ClientConnectionPayload>("username-too-long");
            }

            return Result.Success(payload);
        }
    }
}
