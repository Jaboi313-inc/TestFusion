namespace TestFusion.Core.Interfaces;

public interface ISecretProtector
{
    string Protect(string value);
    string Unprotect(string value);
}