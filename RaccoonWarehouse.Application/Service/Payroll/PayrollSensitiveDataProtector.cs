using System.Security.Cryptography;
using System.Text;
namespace RaccoonWarehouse.Application.Service.Payroll;

public interface IPayrollSensitiveDataProtector { string Protect(string value); string Unprotect(string value); }
public sealed class PayrollSensitiveDataProtector : IPayrollSensitiveDataProtector
{
    private readonly byte[]? _key;
    public PayrollSensitiveDataProtector() { var value=Environment.GetEnvironmentVariable("ROCCOPOS_PAYROLL_ENCRYPTION_KEY");if(!string.IsNullOrWhiteSpace(value)){try{_key=Convert.FromBase64String(value);}catch(FormatException){_key=null;}} }
    public PayrollSensitiveDataProtector(byte[] key)=>_key=key.ToArray();
    private byte[] Key()=>_key?.Length==32?_key:throw new InvalidOperationException("يلزم إعداد مفتاح تشفير مشترك وآمن بطول ٣٢ بايت في ROCCOPOS_PAYROLL_ENCRYPTION_KEY لحفظ أو قراءة رقم الضمان.");
    public string Protect(string value)
    {
        var bytes=Encoding.UTF8.GetBytes(value);var nonce=RandomNumberGenerator.GetBytes(12);var tag=new byte[16];var ciphertext=new byte[bytes.Length];
        using var aes=new AesGcm(Key(),16);aes.Encrypt(nonce,bytes,ciphertext,tag);
        return "v1:"+Convert.ToBase64String(nonce.Concat(tag).Concat(ciphertext).ToArray());
    }
    public string Unprotect(string value)
    {
        if(!value.StartsWith("v1:"))throw new InvalidOperationException("صيغة رقم الضمان المشفر غير مدعومة.");
        var bytes=Convert.FromBase64String(value[3..]);if(bytes.Length<28)throw new InvalidOperationException("رقم الضمان المشفر غير صالح.");
        var plain=new byte[bytes.Length-28];using var aes=new AesGcm(Key(),16);aes.Decrypt(bytes.AsSpan(0,12),bytes.AsSpan(28),bytes.AsSpan(12,16),plain);return Encoding.UTF8.GetString(plain);
    }
}
