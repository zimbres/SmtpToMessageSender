namespace SmtpToMessageSender.Smtp;

public static class IpHelper
{
    public static bool IpMatches(string allowed, IPAddress remoteIp)
    {
        if (!allowed.Contains('/'))
            return IPAddress.Parse(allowed).Equals(remoteIp);

        var parts = allowed.Split('/');
        var baseIp = IPAddress.Parse(parts[0]);
        var prefix = int.Parse(parts[1]);

        var baseBytes = baseIp.GetAddressBytes();
        var remoteBytes = remoteIp.GetAddressBytes();

        int bytes = prefix / 8;
        int bits = prefix % 8;

        for (int i = 0; i < bytes; i++)
            if (baseBytes[i] != remoteBytes[i])
                return false;

        if (bits > 0)
        {
            int mask = 0xFF << (8 - bits);
            if ((baseBytes[bytes] & mask) != (remoteBytes[bytes] & mask))
                return false;
        }

        return true;
    }
}
