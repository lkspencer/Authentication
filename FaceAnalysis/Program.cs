using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace FaceAnalysis {
    class Program {
        static int MAX = 1000;
        static int DISTANCES = 12;
        static float FLOATRANGE = (float).001;
        static string FILENAME = "kirk3.dwt";
         
        static void Main(string[] args) {
            Console.WriteLine("Loading File");
            List<List<DistanceWeightTolerance>> dwtList = LoadDistanceWeightTolerance();
            Console.WriteLine("File Loaded with : " + dwtList.Count + " elements");

            List<List<DistanceWeightTolerance>> pivotedDWTList = new List<List<DistanceWeightTolerance>>();

            for (int i = 0; i < dwtList[0].Count; i++)
            {
                List<DistanceWeightTolerance> tempDwtList = new List<DistanceWeightTolerance>();
                for (int j = 0; j < dwtList.Count; j++)
                {
                    tempDwtList.Add(dwtList[j][i]);
                }
                pivotedDWTList.Add(tempDwtList);
            }



            using (StreamWriter sw = new StreamWriter("BulkClusterData.csv"))
            {
                for (int i = 0; i < pivotedDWTList.Count; i++)
                {

                    int distanceNumber = i + 1;
                    sw.WriteLine("d" + distanceNumber + " number of elements:  " + pivotedDWTList[i].Count);
                    sw.WriteLine("\tFirst Element: " + pivotedDWTList[i].FirstOrDefault().Distance);
                    pivotedDWTList[i] = pivotedDWTList[i].OrderBy(p => p.Distance).ToList();
                    sw.WriteLine("\tSorted");
                    sw.WriteLine("\tFirst Sorted Distance: " + pivotedDWTList[i].FirstOrDefault().Distance);
                    sw.WriteLine("\tClustering Elements");
                    var clusterList = ClusterElements(pivotedDWTList[i]);

                    foreach (Cluster cl in clusterList)
                    {
                        sw.WriteLine("Cluster Count: " + cl.ClusteredElementCount + "\tStart Value: " + cl.StartValue + "\tEnd Value: " + cl.EndValue);
                    }
                }               
            }
            Console.WriteLine("Bulk File Written");
            var jss = new JavaScriptSerializer();
            using (StreamWriter swto = new StreamWriter("Tolerance.json"))
            {
                for (int i = 0; i < pivotedDWTList.Count; i++)
                {
                    //int distanceNumber = i + 1;
                    var clusterList = ClusterElements(pivotedDWTList[i]);
                    Tolerance isavirtue = IdentifyTolerance(clusterList, i);
                    var data = jss.Serialize(isavirtue);
                    swto.WriteLine(data);
                }
            }
            Console.WriteLine("EXCELSIOR!!");  
            Console.ReadKey();



        }

        static public Tolerance IdentifyTolerance(List<Cluster> cl, int index)
        {
            var tol = new Tolerance();
            tol.Index = index;
            double min = 0;
            double max = 0;

            bool minfound = false;
            bool maxfound = false;

            foreach (Cluster c in cl)
            {

                if (c.ClusteredElementCount > 100)
                {
                    if (!minfound)
                    {
                        min = c.StartValue;
                        minfound = true;
                    }
                    if (!maxfound)
                    {
                        max = c.EndValue;
                    }
                }   
            }

            if (max > 0)
            {
                maxfound = true;
            }

            if (minfound && maxfound)
            {
                tol.Max = max;
                tol.Min = min;
            }
            else
            {
                tol.Max = 0;
                tol.Min = 0;
            }

            return tol;
        }

        static public List<List<DistanceWeightTolerance>> LoadDistanceWeightTolerance()
        {
            using (StreamReader r = new StreamReader(FILENAME))
            {
                List<List<DistanceWeightTolerance>> results = new List<List<DistanceWeightTolerance>>();

                while (r.Peek() >= 0)
                {
                    string json = r.ReadLine();
                    List<DistanceWeightTolerance> items = JsonConvert.DeserializeObject<List<DistanceWeightTolerance>>(json);
                    results.Add(items);
                }

                return results;
            }
        }



        //static public bool WriteToCSV(List<List<DistanceWeightTolerance>> dwtsList)
        //{
        //    bool result = false;
        //    using (StreamWriter sw = new StreamWriter("CSVdata.csv"))
        //    {
        //        foreach (List<DistanceWeightTolerance> dwts in dwtsList)
        //        {
        //            var clusterList = ClusterElements(dwts);
        //            string csvLine = ;
        //            foreach (Cluster cl in clusterList)
        //            {
        //                Console.WriteLine("Cluster Count: " + cl.ClusteredElementCount + "\tStart Value: " + cl.StartValue + "\tEnd Value: " + cl.EndValue);
        //            }
        //        }
        //    }
        //    result = true;
        //    return result;
        //}

        static public List<Cluster> ClusterElements(List<DistanceWeightTolerance> dwts)
        {
            var min = dwts.FirstOrDefault().Distance;
            var max = dwts.LastOrDefault().Distance;

            var elementCluster = new List<Cluster>();

            for (double f = min; f < max; f += FLOATRANGE)
            {
                int clusterCount = 0;
                foreach (DistanceWeightTolerance dwt in dwts)
                {
                    if (dwt.Distance >= f && dwt.Distance < f + FLOATRANGE)
                    {
                        clusterCount++;
                    }  
                }

                var charlieFoxtrot = new Cluster();
                charlieFoxtrot.StartValue = (float)f;
                charlieFoxtrot.EndValue = (float)(f + FLOATRANGE);
                charlieFoxtrot.ClusteredElementCount = clusterCount;
                elementCluster.Add(charlieFoxtrot);
            }

            return elementCluster;

        }
    }
}
