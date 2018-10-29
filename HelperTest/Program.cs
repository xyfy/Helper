using System.Text;
using Xyfy.Helper;
namespace HelperTest
{
    class Program
    {
        static void Main(string[] args)
        {

            RSAHelper helper = new RSAHelper(RSAType.RSA2, Encoding.UTF8, "aaaaaaaaaaaaaa", "");
        }
    }
}
