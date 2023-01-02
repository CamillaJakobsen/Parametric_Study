using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FemDesign;
using System.Globalization;
using FemDesignProgram.Helpers;
using FemDesign.Materials;

namespace FemDesign.Optimisation
{
    public partial class Optimisation
    {

        private static void optimisationOfSlab()
        {
            string path = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\sample_optimisation_slab.struxml";
            string bscPathQEconcrete = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\QEconcrete.bsc";
            string bscPathQEreinforcement = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\QEreinforcement.bsc";
            string outFolder = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputs";
            string tempPath = outFolder + "temp.struxml";
            List<string> bscPaths = new List<string>();
            bscPaths.Add(bscPathQEconcrete);
            bscPaths.Add(bscPathQEreinforcement);

            Model model = Model.DeserializeFromFilePath(path);



            // CO2 udledning pr m3 beton med en massefylde på 2246 kg/m3
            double concreteCarbon = 227.07; // C20/25 = 227.07
            // C25/30 = 252.7
            // C30/37 = 293,69
            // C35/45 = 311.37
            // C40/50 = 436.14
            //double concreteCarbon = 20; // C20/25 = 227.07

            //CO2 udledning pr kg armeringsstål
            double reinforcementCarbon = 0.6841;
            //double reinforcementCarbon = 70;
            double concreteVolume = 0;
            double reinforcementWeight = 0;



            //Read slab (hard coded in this example, probably better to look for a slab with a certain name, eg. P.1)
            FemDesign.Shells.Slab slab = model.Entities.Slabs[0];


            //Sets up what type of analysis should be done
            #region Analysis Setup
            // Setup for calculation of load combinations
            bool NLE = true;
            bool PL = false;
            bool NLS = false;
            bool Cr = false;
            bool _2nd = false;

            // Skapar inställningar för analysen
            var combItem = new FemDesign.Calculate.CombItem(0, 0, NLE, PL, NLS, Cr, _2nd);

            int numLoadCombs = model.Entities.Loads.LoadCombinations.Count;
            var combItems = new List<FemDesign.Calculate.CombItem>();
            for (int i = 0; i < numLoadCombs; i++)
            {
                combItems.Add(combItem);
            }

            // Behövs för datastrukturens skull
            Calculate.Comb comb = new Calculate.Comb();
            comb.CombItem = combItems.ToList();

            #endregion

            
            Dictionary<string, double> concreteStrength = new Dictionary<string, double>();

        
        


            


            //Loop variables
            double low = 0.2;
            double high = 1;


            List<double> GWP = new List<double>();

            for (double i = low; i < high; i = i + 0.05)
            {
                //Set thickness of slab
                double thickness = i;
                slab.SlabPart.Thickness[0].Value = Math.Round(thickness, 3);

                //Save temporary model
                model.SerializeModel(tempPath);

                //Run analysis and get results
                RunAnalysis(tempPath, bscPaths);
                concreteVolume = ConcreteVolume();
                reinforcementWeight = ReinforcementWeight();

                //Calculate cost, write to console app and write to list
                double totalGWP = concreteCarbon * concreteVolume + reinforcementCarbon * reinforcementWeight;
                Console.WriteLine(string.Format("{0} {1} {2}", "GWP: ", totalGWP, thickness + "m"));
                GWP.Add(totalGWP);

                foreach 

            }

        }

        public static void RunAnalysis(string modelPath, List<string> bscFilePaths)
        {
            FemDesign.Calculate.Analysis analysis = new FemDesign.Calculate.Analysis(null, null, null, null, true, false, false, false, false, false, false, false, false, false, false, false, false);
            FemDesign.Calculate.Design design = new FemDesign.Calculate.Design(true, false);
            FemDesign.Calculate.FdScript fdScript = FemDesign.Calculate.FdScript.Design("rc", modelPath, analysis, design, bscFilePaths, "", true);
            FemDesign.Calculate.Application app = new FemDesign.Calculate.Application();
            app.RunFdScript(fdScript, false, true, true);

        }

        public static double ConcreteVolume()
        {
            //Read results from csv file
            double concreteVolume = 0;
            int counter = 0;
            using (var reader = new StreamReader(@"C:\femdesign-api\Optimisation\Outputstemp\results\QEconcrete.csv"))
            {

                //Console.WriteLine("");
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split("\t");
                    if (values[0] == "TOTAL" & line != "")
                    {
                        //Console.WriteLine(string.Format("{0} {1} {2}", values[0], "concrete", values[10]));
                        concreteVolume = double.Parse(values[8], CultureInfo.InvariantCulture);
                    }
                    counter++;
                }
            }

            return concreteVolume;
        }

        public static double ReinforcementWeight()
        {
            //Read results from csv file
            double reinforcementWeight = 0;
            int counter = 0;
            using (var reader = new StreamReader(@"C:\femdesign-api\Optimisation\Outputstemp\results\QEreinforcement.csv"))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split("\t");
                    if (values[0] == "TOTAL" & line != "")
                    {
                        //Console.WriteLine(string.Format("{0} {1} {2}", values[0], "reinforcement", values[5]));
                        reinforcementWeight = tTokgConverter.Convert(double.Parse(values[5], CultureInfo.InvariantCulture));
                    }
                    counter++;
                }
            }
            return reinforcementWeight;
        }

        
        
    }
}
