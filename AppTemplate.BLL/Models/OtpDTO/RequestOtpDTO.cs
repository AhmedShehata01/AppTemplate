using AppTemplate.DAL.Enum;

public class OtpDTO
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public int Attempts { get; set; }
    public bool IsUsed { get; set; }
    public OtpPurpose Purpose { get; set; } = OtpPurpose.Login;
    public DateTime Expiry { get; set; }
}

public class RequestOtpDTO
{
    public string Email { get; set; } = null!;
    public OtpPurpose Purpose { get; set; } = OtpPurpose.Login;
}

public class VerifyOtpDTO
{
    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;
    public OtpPurpose Purpose { get; set; } = OtpPurpose.Login;
}
