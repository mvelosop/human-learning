using System.Threading.Tasks;

namespace AlexaBotApp.Phonemizer
{
    public interface IPhonemizerService
    {
        Task<string> GetPhonemesAsync(string word);
    }
}