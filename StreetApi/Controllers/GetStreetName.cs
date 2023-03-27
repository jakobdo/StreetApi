using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.Text;

namespace StreetApi.Controllers
{
    [Route("api/streets")]
    [ApiController]
    public class Streets : Controller
    {
        [HttpGet("{StreetCode}/{MunicipalityCode}")]
        public IActionResult GetStreetName(int StreetCode, int MunicipalityCode)
        {
            String result = "";
            using (var connection = new SqliteConnection("Data Source=streets.db"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"SELECT Name from Addresses WHERE Id = $id AND MunicipalityCode = $municipality_code LIMIT 1";
                command.Parameters.AddWithValue("$id", StreetCode);
                command.Parameters.AddWithValue("$municipality_code", MunicipalityCode);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result = reader.GetString(0);
                    }
                }
            }
            if( result == "")
            {
                return NotFound();
            }
            return Ok(result);
        }

        [HttpGet("search/{Letters}")]
        public IActionResult SearchStreets(string Letters)
        {
            List<string> results = new List<string>();
            var unique = new SortedSet<char>(Letters);
            using (var connection = new SqliteConnection("Data Source=streets.db"))
            {
                connection.Open();

                // We should make sure only Letters A-Z, ÆØÅ etc is searched!
                foreach (char Letter in unique)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = @"SELECT DISTINCT	Name from Addresses WHERE Name LIKE $start_letter ORDER BY Name";
                    command.Parameters.AddWithValue("$start_letter", Letter.ToString().ToUpper() +  "%");

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(reader.GetString(0));
                        }
                    }
                }
            }
            return Ok(results);
        }

        [HttpPost("update")]
        public IActionResult UpdateDatabase()
        {
            using(var connection = new SqliteConnection("Data Source=streets.db"))
            {
                connection.Open();

                // Lets create the table, if it does not exists
                var command = connection.CreateCommand();
                command.CommandText = @"CREATE TABLE IF NOT EXISTS Addresses(
                    [Id] INTEGER NOT NULL, 
                    [MunicipalityCode] INTEGER NOT NULL, 
                    [Updated] DATETIME NOT NULL, 
                    [ToMunicapalityCode] INTEGER NOT NULL,
                    [ToStreetCode] INTEGER NOT NULL,
                    [FromMunicapalityCode] INTEGER NOT NULL,
                    [FromStreetCode] INTEGER,
                    [StartDate] DATETIME NOT NULL,
                    [StreetNameShort] NVARCHAR(20) NOT NULL,
                    [Name] NVARCHAR(40) NOT NULL, 
                    PRIMARY KEY(Id, MunicipalityCode)
                )";
                command.ExecuteNonQuery();

                command.CommandText = @"INSERT INTO Addresses VALUES ($id, $municipality_code, $updated, $tocode, $tostreet, $fromcode, $fromstreet, $start, $short, $name)";

                var parameter_id = command.CreateParameter();
                parameter_id.ParameterName = "$id";
                command.Parameters.Add(parameter_id);

                var parameter_mun_code = command.CreateParameter();
                parameter_mun_code.ParameterName = "$municipality_code";
                command.Parameters.Add(parameter_mun_code);

                var parameter_updated = command.CreateParameter();
                parameter_updated.ParameterName = "$updated";
                command.Parameters.Add(parameter_updated);

                var parameter_tocode = command.CreateParameter();
                parameter_tocode.ParameterName = "$tocode";
                command.Parameters.Add(parameter_tocode);

                var parameter_tostreet = command.CreateParameter();
                parameter_tostreet.ParameterName = "$tostreet";
                command.Parameters.Add(parameter_tostreet);

                var parameter_fromcode = command.CreateParameter();
                parameter_fromcode.ParameterName = "$fromcode";
                command.Parameters.Add(parameter_fromcode);

                var parameter_fromstreet = command.CreateParameter();
                parameter_fromstreet.ParameterName = "$fromstreet";
                command.Parameters.Add(parameter_tocode);

                var parameter_start = command.CreateParameter();
                parameter_start.ParameterName = "$start";
                command.Parameters.Add(parameter_start);

                var parameter_short = command.CreateParameter();
                parameter_short.ParameterName = "$short";
                command.Parameters.Add(parameter_short);

                var parameter_name = command.CreateParameter();
                parameter_name.ParameterName = "$name";
                command.Parameters.Add(parameter_name);

                string path = @"Data\A370715.txt";
                foreach (string line in System.IO.File.ReadLines(path, Encoding.Latin1))
                {
                    if (line.StartsWith("001"))
                    {
                        // Parse logic
                        // 001 = Streetnames
                        // KOMKOD N 4 4 Kommunekode
                        // VEJKOD N 4 8 Vejkode
                        // TIMESTAMP A 12 12 Ajourføringstidspunktet
                        // TILKOMKOD N 4 24 Vejen fortsætter til kommune
                        // TILVEJKOD N 4 28 Vejen fortsætter til vej
                        // FRAKOMKOD N 4 32 Vejen kommer fra komkod
                        // FRAVEJKOD N 4 36 Vejen kommer fra vejkod
                        // HAENSTART N 12 40 Startdato - ÅÅÅÅMMDDTTMM
                        // VEJADRNVN A 20 52 Vejadresseringsnavn
                        // VEJNVN A 40 72 Vejnavn
                        // SubString is 0-index based!
                        int MunicapalityCode = Int32.Parse(line.Substring(3, 4));
                        int StreetCode = Int32.Parse(line.Substring(7, 4));
                        string Updated = DateTime.ParseExact(line.Substring(11, 12), "yyyyMMddHHmm", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd HH:mm:ss");
                        int ToMunicapalityCode = Int32.Parse(line.Substring(23, 4));
                        int ToStreetCode = Int32.Parse(line.Substring(27, 4));
                        int FromMunicapalityCode = Int32.Parse(line.Substring(31, 4));
                        int FromStreetCode = Int32.Parse(line.Substring(35, 4));
                        string StartDate = DateTime.ParseExact(line.Substring(39, 12), "yyyyMMddHHmm", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd HH:mm:ss");
                        string StreetNameShort = line.Substring(51, 20);
                        string StreetName = line.Substring(71, 40).Trim();

                        // Console.WriteLine(MunicapalityCode.ToString());
                        // Console.WriteLine(StreetCode.ToString());
                        // Console.WriteLine(Updated.ToString());
                        // Console.WriteLine(StreetName);
                        parameter_id.Value = StreetCode;
                        parameter_mun_code.Value = MunicapalityCode;
                        parameter_updated.Value = Updated;
                        parameter_tocode.Value = ToMunicapalityCode;
                        parameter_tostreet.Value = ToStreetCode;
                        parameter_fromcode.Value = FromMunicapalityCode;
                        parameter_fromstreet.Value = FromStreetCode;
                        parameter_start.Value = StartDate;
                        parameter_short.Value = StreetNameShort;
                        parameter_name.Value = StreetName;
                        try
                        {
                            command.ExecuteNonQuery();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                            Console.WriteLine(line);
                        }
                    }
                }
            }
            return Ok(true);
        }
    }
}
