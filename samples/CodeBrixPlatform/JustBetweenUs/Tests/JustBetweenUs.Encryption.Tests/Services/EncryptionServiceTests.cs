using SilverAssertions;
using JustBetweenUs.Encryption.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace JustBetweenUs.Encryption.Tests.Services;

public class EncryptionServiceTests : IClassFixture<EncryptionTestingFixture>
{
    private readonly EncryptionTestingFixture _fixture;
    private readonly ITestOutputHelper _output;

    private IEncryptionService GetService() => _fixture.GetService<IEncryptionService>() as EncryptionService;

    public EncryptionServiceTests(EncryptionTestingFixture fixture,
        ITestOutputHelper output)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        _output = new SimpleTestOutputHelper(output);

        fixture.CreateAndRegisterLogger<EncryptionService>(_output);
    }
    
    private string CleanLineEndings(string text) => 
        (string.IsNullOrEmpty(text)) 
            ? text 
            : text.Replace("\r\n", "\n");

    [Fact]
    public void can_get_service() => GetService().Should().NotBeNull();

    [Fact]
    public async Task GetDefaultKey_retrieves_key() =>
        (await GetService().GetDefaultKey()).Should().NotBeNullOrEmpty();

    [Theory]
    [InlineData("27544076",
@"Hello world.
This is a test of Triple DES encryption.
Triple DES is no longer considered to be secure by the NIST (as of 2017).
https://en.wikipedia.org/wiki/Triple_DES#Security",
"mf33rxktmexB05ccuAqIMMiYq9he3PJK+HjjASpml1nEjx3+5IqijX/fp12b8OimzqXUrlNtbc7z5u2yV8CXILv0wGnp16vaX8HgWV8UfeHWX3dPCsIOoG0ZJS5Xvh+jNLJj+zke9GQkvsZzK0vQst29k/KyTq7SH7Mw15KH8WnRUhPKOfYZqow3knuzSA0C5QGpOed6Y1+RvGNQf63vL+5JGELCpoIwG78o0Sh+Y0Qa8V6SIl2t/Q==")]
    public async Task OriginalTripleDES_EncryptToBase64_can_encrypt(
        string key, string toEncrypt, string expectedEncrypted)
    {
        //Arrange
        var crypt = GetService();

        //Act
        var encrypted = await crypt.OriginalTripleDES_EncryptToBase64(key, CleanLineEndings(toEncrypt));

        //Assert
        encrypted.Should().Be(expectedEncrypted);

        //Let's make sure that we get the same text after decryption
        var decrypted = await crypt.OriginalTripleDES_DecryptFromBase64(key, encrypted);
        CleanLineEndings(decrypted).Should().Be(CleanLineEndings(toEncrypt));

        //Report
        _output.WriteLine($"Using the key: {key}");
        _output.WriteLine($"\nThe message:\n{toEncrypt}");
        _output.WriteLine($"\nWas encrypted to:\n{encrypted}");
    }

    [Theory]
    [InlineData("27544076",
"8NyqFvVx6VYo17mdk0htiYWHZs+FD+vjp3oAdzIxWnNH62IQ/m0KJAgDa5lBOPfSQl4uov6LR6uq+wjYHwRCMypPxxJXJcuNnymQhSGKKrj9dB2OYdaUx8YwNd0nQi6Y1lr7EDthH4MMzuHdK6EOsgGVZNgbJs1gO+cidQX6BibawN/GD11N0gmFZ5WaGj+mpuqPwb+VK4ge3/CczcUEvAgpVIcdVY7f",
@"You have correctly decrypted the secret message!
The history of encryption and secret messages is VERY interesting:
https://en.wikipedia.org/wiki/Encryption#History")]
    public async Task OriginalTripleDES_DecryptFromBase64_can_decrypt(
        string key, string toDecrypt, string expectedDecrypted)
    {
        //Arrange
        var crypt = GetService();

        //Act
        var decrypted = await crypt.OriginalTripleDES_DecryptFromBase64(key, toDecrypt);

        //Assert
        CleanLineEndings(decrypted).Should().Be(CleanLineEndings(expectedDecrypted));

        //Let's make sure that we get the same encrypted text if we re-encrypt it
        var encrypted = await crypt.OriginalTripleDES_EncryptToBase64(key, decrypted);
        encrypted.Should().Be(toDecrypt);

        //Report
        _output.WriteLine($"Using the key: {key}");
        _output.WriteLine($"\nThe encrypted text:\n{toDecrypt}");
        _output.WriteLine($"\nWas decrypted to:\n{decrypted}");
    }

    [Theory]
    [InlineData("27544076",
        @"AES (a.k.a. Rijndael) is quite a good, secure encryption algorithm; especially since we are using a randomized initialization vector.
https://en.wikipedia.org/wiki/Advanced_Encryption_Standard
https://en.wikipedia.org/wiki/Initialization_vector")]
    public async Task AES_EncryptToBase64_can_encrypt(string key, string toEncrypt)
    {
        //Arrange
        var crypt = GetService();

        //Act
        var encrypted = await crypt.AES_EncryptToBase64(key, toEncrypt);

        //Assert
        
        //With this encryption method, we can't know in advance what the encrypted
        //  bytes will look like - which is good! more secure! - because of the
        //  randomized initialization vector, but that means we can't compare the
        //  encrypted bytes to an expected result.
        //encrypted.Should().Be(expectedEncrypted);

        //Let's make sure that we get the same text after decryption
        var decrypted = await crypt.AES_DecryptFromBase64(key, encrypted);
        decrypted.Should().Be(toEncrypt);

        //Report
        _output.WriteLine($"Using the key: {key}");
        _output.WriteLine($"\nThe message:\n{toEncrypt}");
        _output.WriteLine($"\nWas encrypted to:\n{encrypted}");
    }

    [Theory]
    [InlineData("27544076",
        "b0i/ESAGVpJeXb4M1G6WSn90b9FD+USbDS0lfeqNmRXNYYfcRKDXiOGObJ+cPSsIdYD+icTy7wEgh0Kmgt2MZUqUenokqZFsd4LHsc9pEDpKo8GAOX1E0ec3PzWeHZyI4pSWeizD1BSyJwNCq7QP5gARE1/K955ROHA2zpjC9BV978S5rWDL0uXDVsKb9F1+yXiShh96fdU9WtJNIQFSTIVhRnJ34DhYOxXhHbVUS3bWNelhX08zwTC5d6DoqdOo2fAwWbsKJZcJSp088V+vKS9LPDfp/MVr+bohkrVPTMqmezyb4s8oCU/gNMIvR4XIxUotDFZuaaXx7oIvIiSHxMfah0paQFYelTeoLtlOIqWYaCHlNGqNlwq92Ht9JMToMmS8DaIJA5SO8Sz/nBiSMA==",
        @"AES is an example of a 'symmetric-key' encryption algorithm; where both the sender and the receiver use the same key to encrypt/decrypt the message - i.e. it is not an example of a public/private key encryption algorithm.
https://en.wikipedia.org/wiki/Symmetric-key_algorithm")]
    public async Task AES_DecryptFromBase64_can_decrypt(
        string key, string toDecrypt, string expectedDecrypted)
    {
        //Arrange
        var crypt = GetService();

        //Act
        var decrypted = await crypt.AES_DecryptFromBase64(key, toDecrypt);

        //Assert
        CleanLineEndings(decrypted).Should().Be(CleanLineEndings(expectedDecrypted));

        //Report
        _output.WriteLine($"Using the key: {key}");
        _output.WriteLine($"\nThe encrypted text:\n{toDecrypt}");
        _output.WriteLine($"\nWas decrypted to:\n{decrypted}");
    }

    [Theory]
    [InlineData("27544076", "This is a test.")]
    public async Task AES_decrypt_tolerates_stray_control_chars_from_clipboard(string key, string message)
    {
        //Arrange - reproduce the Intel/x64 macOS clipboard glitch where an invisible
        //  U+0001 control character was being prepended to the pasted Base64 text,
        //  which made IsBase64Text() return false and blocked decryption.
        var crypt = GetService();
        var encrypted = await crypt.AES_EncryptToBase64(key, message);
        var corrupted = "\u0001" + encrypted; //stray SOH char at index 0, as seen in the [DIAG] output

        //Act + Assert - the corrupted text must still be recognized as encrypted...
        crypt.IsBase64Text(corrupted).Should().BeTrue();

        //...and must still decrypt back to the original message.
        var decrypted = await crypt.AES_DecryptFromBase64(key, corrupted);
        decrypted.Should().Be(message);
    }

    [Theory]
    [InlineData("27544076",
    @"Twofish is a very powerful symmetric key encryption algorithm. It was a strong contender for being chosen as the AES standard encryption algorithm; but was beat out by Rijndael on performance (supposedly).
It has never been cracked, as far as we know.
https://en.wikipedia.org/wiki/Twofish")]
    public async Task Twofish_EncryptToBase64_can_encrypt(string key, string toEncrypt)
    {
        //Arrange
        var crypt = GetService();

        //Act
        var encrypted = await crypt.Twofish_EncryptToBase64(key, toEncrypt);

        //Assert
        //We can't really check to see if the encrypted value is valid, without decrypting it
        var decrypted = await crypt.Twofish_DecryptFromBase64(key, encrypted);
        decrypted.Should().Be(toEncrypt);

        //Report
        _output.WriteLine($"Using the key: {key}");
        _output.WriteLine($"\nThe message:\n{toEncrypt}");
        _output.WriteLine($"\nWas encrypted to:\n{encrypted}");
    }

    [Theory]
    [InlineData("27544076",
        "U1MFOx87uh5ITQOG7sxRmMkEOTlCErCnMN4NqEc23ltGrq9g7q/FJMSu4pL9XWoNoSwy2WrSvziVXzpklrMAbapkBCtYRpHpXbT5ZioMTcSkOKef92OYoLb/uBGVom63yJDyPZWQsbKyvk+xN1uPJIlXvLfSouC7MwArEE8bO8TOoHcyAHPA2RNgxfsl7QNH4tiOuB2OitMoolVdSXSlyw4SRs2LOVO2IOx+xtPHMgpFvALnXws1q3ovxG9dYVSA7hhVQZ1o+AeP1h/OXpSm/oHaBZEXWOLEiyzjsTAU/eRmj9fj2YwajO3wFa6vH9ULQp7i00/4FoIk+oJKH9dadw==",
        @"The Twofish encryption algorithm was created by Bruce Schneier in the late 1990s.
Mr. Schneier is one of the top cryptography experts in the world, and - frankly - a god-among-men.
https://en.wikipedia.org/wiki/Bruce_Schneier")]
    public async Task Twofish_DecryptFromBase64_can_decrypt(
        string key, string toDecrypt, string expectedDecrypted)
    {
        //Arrange
        var crypt = GetService();

        //Act
        var decrypted = await crypt.Twofish_DecryptFromBase64(key, toDecrypt);

        //Assert
        CleanLineEndings(decrypted).Should().Be(CleanLineEndings(expectedDecrypted));

        //Report
        _output.WriteLine($"Using the key: {key}");
        _output.WriteLine($"\nThe encrypted text:\n{toDecrypt}");
        _output.WriteLine($"\nWas decrypted to:\n{decrypted}");
    }
}
