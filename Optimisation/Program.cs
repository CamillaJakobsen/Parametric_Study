using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FemDesign;
using System.Globalization;
//using FemDesignProgram.Helpers;
using Optimisation;
using System.ComponentModel;
using FemDesign.Materials;
using FemDesign.Results;

namespace FemDesign.Examples
{
    public class Optimisation_Deck
    {

        public static void Main(string[] args)
        {
            //string path = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\sample_optimisation_slab.struxml";
            string path = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\sample_optimisation_slab_custom.struxml";
            //string path = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\FEM-design_files\fem-climate-example_reinforcement.struxml";
        
            string bscPathQEconcrete = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\QEconcrete.bsc";
            string bscPathQEreinforcement = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\QEreinforcement.bsc";
            string outFolder = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputs";
            string tempPath = outFolder + "temp.struxml";
            List<string> bscPaths = new List<string>();
            //bscPaths.Add(bscPathQEconcrete);
            //bscPaths.Add(bscPathQEreinforcement);
            string bscPath = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\quantities_test.bsc";

            Model model = Model.DeserializeFromFilePath(path);

            var resultTypes = new List<Type>
            {
                typeof(Results.QuantityEstimationConcrete),
                typeof(Results.QuantityEstimationReinforcement),
                typeof(Results.RCShellUtilization),
            };

            // Creating the bsc paths in C:\femdesign-api\quantities_test\scripts
            List<string> bscPathsFromResultTypes = Calculate.Bsc.BscPathFromResultTypes(resultTypes, bscPath);


            // CO2 udledning pr m3 beton med en massefylde på 2246 kg/m3
            Dictionary<string, double> concreteStrength = new Dictionary<string, double>();
            concreteStrength.Add("C20/25", 227.07);
            concreteStrength.Add("C25/30", 252.7);
            concreteStrength.Add("C30/37", 293.69);
            concreteStrength.Add("C35/45", 311.47);
            concreteStrength.Add("C40/50", 440.77);

            //CO2 udledning pr kg armeringsstål
            double reinforcementCarbon = 0.6841;

            //double reinforcementCarbon = 70;
            double concreteVolume = 0;
            double reinforcementWeight = 0;
            double utilisation = 0;
            string utilisationSC = "";


            //Read slab (hard coded in this example, probably better to look for a slab with a certain name, eg. P.1)
            //Shells.Slab slab = model.Entities.Slabs[0];


            //Sets up what type of analysis should be done
            #region Analysis Setup
            
            // Setup for calculation of load combinations
            bool NLE = true; // Non-linear elastic calculation
            bool PL = false; // Plastic analysis
            bool NLS = false; // Non-linear soil
            bool Cr = false; // Cracked-section analysis
            bool _2nd = false; // Second order analysis

            // Skapar inställningar för analysen
            var combItem = new Calculate.CombItem(0, 0, NLE, PL, NLS, Cr, _2nd);

            int numLoadCombs = model.Entities.Loads.LoadCombinations.Count;
            var combItems = new List<Calculate.CombItem>();
            for (int i = 0; i < numLoadCombs; i++)
            {
                combItems.Add(combItem);
            }

            // Behövs för datastrukturens skull
            Calculate.Comb comb = new Calculate.Comb();
            comb.CombItem = combItems.ToList();

            #endregion


            //Loop variables
            double low = 0.18;
            double high = 0.31;

            //Second Loop

            
            List<double> concreteVolumeList = new List<double>();
            List<double> reinforcementWeightList = new List<double>();
            List<double> utilisationList = new List<double>();

            List<double> GWP = new List<double>();

            foreach (KeyValuePair<string, double> entry in concreteStrength)
            {
                //var materialsDB = Materials.MaterialDatabase.DeserializeStruxml("materials.struxml");
                //var material = materialsDB.MaterialByName("C35/45");
                var materialInput = entry.Key;
                var materialsDatabase = Materials.MaterialDatabase.DeserializeStruxml("materials.struxml");
                //Materials.Material newMaterial = materialsDatabase.MaterialByName(materialInput);
                Materials.Material newMaterial = materialsDatabase.MaterialByName(materialInput);


                for (double i = low; i < high; i = i + 0.01)
                {
                    //Set thickness of slab
                    double thickness = i;

                    //slab.Material = newMaterial;

                    model.Materials.Material[0].Concrete = newMaterial.Concrete;
                    Materials.Material materialSlab = model.Entities.Slabs[0].Material;
                    //materialSlab = newMaterial;
                    Shells.Slab slab = model.Entities.Slabs[0];
                    slab.SlabPart.Thickness[0].Value = Math.Round(thickness, 3);
                    //Save temporary model
                    //model.SerializeModel(tempPath);
                    string outPathIndividual = outFolder + "sample_slab_out" + ".struxml";
                    model.SerializeModel(outPathIndividual);

                    //Run analysis and get results

                    RunAnalysis(outPathIndividual, bscPathsFromResultTypes);
                    concreteVolume = ConcreteVolume();
                    concreteVolumeList.Add(concreteVolume);
                    reinforcementWeight = ReinforcementWeight();
                    reinforcementWeightList.Add(reinforcementWeight);

                    utilisation = Utilisation();
                    utilisationList.Add(utilisation);

                    utilisationSC = UtilisationSC();

                    double concreteCarbon = entry.Value;

                    double reinforcementRatio = (reinforcementWeight / 7850) / concreteVolume;

                    //Calculate GWP, write to console app and write to list
                    double totalGWP = concreteCarbon * concreteVolume + reinforcementCarbon * reinforcementWeight;
                    Console.WriteLine(string.Format("{0} {1} {2} {3} {4} {5} {6}", "GWP: ", totalGWP, thickness + "m", materialInput, utilisation, utilisationSC, reinforcementRatio));
                    GWP.Add(totalGWP);

                }


                Console.WriteLine("end");
            }

        }
    
        public static void RunAnalysis(string modelPath, List<string> bscFilePaths)
        {
            Calculate.Analysis analysis = new Calculate.Analysis(null, null, null, null, false, false, false, false, false, false, false, false, true, false, false, false, false);
            Calculate.Design design = new Calculate.Design(autoDesign: true, check: true, applyChanges: true);
            Calculate.FdScript fdScript = Calculate.FdScript.Design("rc", modelPath, analysis, design, bscFilePaths, "", true);
            Calculate.Application app = new Calculate.Application();
            app.RunFdScript(fdScript, false, true, true);

            

        }

        public static double ConcreteVolume()
        {
            //Read results from csv file
            double concreteVolume = 0;
            int counter = 0;
            using (var reader = new StreamReader(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\QuantityEstimationConcrete.csv"))
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
            using (var reader = new StreamReader(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\QuantityEstimationReinforcement.csv"))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split("\t");
                    if (values[0] == "TOTAL" & line != "")
                    {
                        //Console.WriteLine(string.Format("{0} {1} {2}", values[0], "reinforcement", values[5]));
                        reinforcementWeight = Double.Parse(values[5].Replace('.', '.'), CultureInfo.InvariantCulture);
                    }
                    counter++;
                }
            }
            return reinforcementWeight;
        }

        public static double Utilisation()
        {
            //Read results from csv file
            double utilisation = 0;
            int counter = 0;
            using (var reader = new StreamReader(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\RCDesignShellUtilizationLoadCombination.csv"))
            {

                //Console.WriteLine("");
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split("\t");
                    if (values[0] == "P.1.1" & line != "")
                    {
                        //Console.WriteLine(string.Format("{0} {1} {2}", values[0], "concrete", values[10]));
                        utilisation = double.Parse(values[1], CultureInfo.InvariantCulture);
                    }
                    counter++;
                }
            }

            return utilisation;

        }

        public static string UtilisationSC()
        {
            //Read results from csv file

            string utilisationSC = "";
            int counter = 0;
            using (var reader = new StreamReader(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\RCDesignShellUtilizationLoadCombination.csv"))
            {

                //Console.WriteLine("");
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split("\t");
                    if (values[0] == "P.1.1" & line != "")
                    {
                        //Console.WriteLine(string.Format("{0} {1} {2}", values[0], "concrete", values[10]));

                        utilisationSC = values[7];
                    }
                    counter++;
                }
            }

            return utilisationSC;
        }
        
    }
}