using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AlexaBotApp.Phonemizer
{
    public class PhonemizerService : BackgroundService, IPhonemizerService
    {
        private readonly IHostingEnvironment _environment;
        private readonly StringBuilder _errorBuilder;
        private readonly Process _espeak;
        private readonly ILogger<PhonemizerService> _logger;

        public PhonemizerService(
            ILogger<PhonemizerService> logger,
            IHostingEnvironment environment)
        {
            _logger = logger;
            _environment = environment;

            _espeak = new Process();
            _errorBuilder = new StringBuilder();
        }

        public async Task<string> GetPhonemesAsync(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) throw new ArgumentException("must be non empty", nameof(word));

            _logger.LogInformation("----- Getting phonemes for \"{Word}\"", word);

            _espeak.StandardInput.WriteLine(word.ToLowerInvariant().Trim(' ', '.'));

            var phonemes = await _espeak.StandardOutput.ReadLineAsync();

            _logger.LogInformation("----- Phonemes for \"{Word}\" => \"{Phonemes}\"", word, phonemes);

            return phonemes;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("----- Starting espeak-ng process...");

            var programDirectory = Path.Combine(_environment.ContentRootPath, "Phonemizer", "espeak-ng");
            var dataDirectory = Path.Combine(programDirectory, "espeak-ng-data");

            var arguments = new StringBuilder();
            arguments.Append($" -ves-419");
            arguments.Append($" -q -x --ipa=2");
            arguments.Append($" --stdout");
            arguments.Append($" --path=\"{dataDirectory}\"");

            _espeak.StartInfo.FileName = Path.Combine(programDirectory, "espeak-ng.exe");
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
                _espeak.Close();
                _logger.LogInformation("----- espeak-ng process stopped.");
            });

            return Task.CompletedTask;
        }
    }
}