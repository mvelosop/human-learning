using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AlexaBotApp.Phonemes
{
    public class Phonemizer : BackgroundService
    {
        private readonly IHostingEnvironment _environment;
        private readonly Process _espeak;
        private readonly ILogger<Phonemizer> _logger;

        public Phonemizer(
            ILogger<Phonemizer> logger,
            IHostingEnvironment environment)
        {
            _logger = logger;
            _environment = environment;

            _espeak = new Process();
        }

        public string GetPhonemesAsync(string word)
        {
            var phonemes = string.Empty;

            lock (this)
            {
                _logger.LogInformation("----- Getting phonemes for \"{Word}\"", word);

                _espeak.StandardInput.WriteLine(word.ToLowerInvariant().Trim(' ', '.'));
                _espeak.StandardInput.Flush();

                Task.Delay(500).Wait();

                phonemes = _espeak.StandardOutput.ReadLine();

                _logger.LogInformation("----- Phonemes for \"{Word}\" => \"{Phonemes}\"", word, phonemes);
            }

            return phonemes;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("----- Starting espeak-ng process...");

            var espeakDirectory = Path.Combine(_environment.ContentRootPath, "Phonemes", "espeak-ng");

            var arguments = new StringBuilder();
            arguments.Append($" -ves-419");
            arguments.Append($" -q -x --ipa=2");
            arguments.Append($" --stdout");

            _espeak.StartInfo.FileName = Path.Combine(espeakDirectory, "espeak-ng.exe");
            _espeak.StartInfo.StandardInputEncoding = Encoding.UTF8;
            _espeak.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            _espeak.StartInfo.Arguments = arguments.ToString();
            _espeak.StartInfo.UseShellExecute = false;
            _espeak.StartInfo.RedirectStandardInput = true;
            _espeak.StartInfo.RedirectStandardOutput = true;
            _espeak.StartInfo.CreateNoWindow = true;

            _espeak.Start();

            stoppingToken.Register(() =>
            {
                _logger.LogInformation("----- Stopping espeak-ng process...");
                _espeak.Kill();
            });

            return Task.CompletedTask;
        }
    }
}