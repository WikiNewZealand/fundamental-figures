using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FigureNZ.FundamentalFigures.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string json = File.ReadAllText(args[0]);

            Figure figure = JsonConvert.DeserializeObject<Figure>(json);

            string term = args[1];

            using (FileStream output = new FileStream($"{term}.xlsx", FileMode.Create))
            {
                (await new XlsxGenerator().FromFigure(figure, term)).CopyTo(output);
            }
        }
    }
}
