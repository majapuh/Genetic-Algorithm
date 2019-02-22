using System;
using System.Linq;


namespace dz4
{
    public class GA
    {
        int N;
        double lowerBound;
        double upperBound;

        double evaluation;
        bool display;
        double pMut;
        int t;

        double[] fi;
        double[][] population;
        int iteration = 0;
        int hits = 0;

        Random random;

        public GA (int _n, double _lowerBound, double _upperBound, double _evaluation, bool _display, double _pMutation, int _t = 3)
        {
            N = _n;
            lowerBound = _lowerBound;
            upperBound = _upperBound;
            evaluation = _evaluation;
            display = _display;
            pMut = _pMutation;
            t = _t;
        }

        public double StartGA (IFunction f, int variableSize)
        {
            random = new Random();
            population = MakePopulation(variableSize);
            double[] xworst, xbest;
            double fxbest, fxworst;
            fi = new double[N];

            //calculate best and worst chromosome
            xworst = (double[])population[0].Clone();
            xbest = (double[])population[0].Clone();

            for (int i = 1; i < N; i++)
            {
                if (f.Value(population[i]) < f.Value(xbest))
                {
                    xbest = population[i];
                }
                else if (f.Value(population[i]) > f.Value(xworst))
                {
                    xworst = population[i];
                }
            }

            //calculate fitness for chromosomes
            fxbest = f.Value(xbest);
            Console.WriteLine("Trenutni fmin = {0}", fxbest);
            fxworst = f.Value(xworst);
            CalculateFitness(f, fxworst, fxbest);

            int indeksW;
            bool change;

            do
            {
                iteration++;
                change = false;
                indeksW = Tournament(f, population);

                double trenutnaVR = f.Value(population[indeksW]);

                if (trenutnaVR < fxbest)
                {
                    xbest = (double[])population[indeksW].Clone();
                    fxbest = trenutnaVR;
                    CalculateFitness(f, fxworst, fxbest);
                    change = true;
                }
                else if (trenutnaVR > fxworst)
                {
                    xworst= (double[])population[indeksW].Clone();
                    fxworst = trenutnaVR;
                    CalculateFitness(f, fxworst, fxbest);
                }
                else
                {
                    fi[indeksW] = Fitness(f, population[indeksW], fxworst, fxbest);
                }

                if (change)
                {
                    Console.WriteLine("Trenutni fmin = {0} || broj evaluacija {1}", fxbest, f.Eval());
                }

                if (fxbest<1e-6 && change)
                {
                    hits++;  
                }

            } while (f.Eval()  < evaluation);
            //  } while (fxbest > 1e-6);
            

            return fxbest;
        }


        int Tournament(IFunction f, double[][] population)
        {
            int[] indexes = new int[t];
            double worst;
            int worstInd;

            bool sameIndex;
            for (int i = 0; i < t; i++)
            {
                do
                {
                    sameIndex = false;
                    indexes[i] = random.Next(0, N);
                    for (int k = 0; k < i; k++)
                    {
                        if (indexes[i] == indexes[k])
                        {
                            sameIndex = true;
                            break;
                        }
                       
                    }
                } while (sameIndex == true);               
            }

            worst = fi[indexes[0]];
            worstInd = indexes[0];
            for (int i = 1; i < t; i++)
            {
                
                if (fi[indexes[i]] < worst)
                {
                    worst = fi[indexes[i]];
                    worstInd = indexes[i];
                }
            }
            double[] x = (double[])population[worstInd].Clone();

            double[][] xparents = new double[2][];
            int[] parentsInd = new int[2];
            int j = 0;
            for (int i = 0; i < t; i++)
            {
                if (indexes[i] != worstInd)
                {
                    xparents[j] = population[indexes[i]];
                    parentsInd[j] = indexes[i];
                    j++;
                }
                if (j == 2) break;
                    
            }

            if (display)
            {
                if (fi[parentsInd[0]] > fi[parentsInd[1]])
                {
                    x = HeuristicCrossover(xparents[1], xparents[0]);
                    population[worstInd] = x;
                }

                else
                {
                    x = HeuristicCrossover(xparents[0], xparents[1]);
                    population[worstInd] = x;
                }

                //constraints check
                for (int i = 0; i < population[worstInd].Count(); i++)
                {
                    if (population[worstInd][i] < lowerBound) population[worstInd][i] = lowerBound;
                    else if (population[worstInd][i] > upperBound) population[worstInd][i] = upperBound;
                }
            }
            else
            {
                   x = AritmeticCrossover(xparents[0], xparents[1]);
                  population[worstInd] = x;
            }


            //Mutation
            x = Mutation_improved(population[worstInd], f);
            population[worstInd] = x;

            return worstInd;
        }

        double[] AritmeticCrossover(double[] x1, double[] x2)
        {
            double a;
            double[] newX = new double[x1.Count()];

            a = random.NextDouble();
            for (int i = 0; i < x1.Count(); i++)
            {  
                newX[i] = x1[i]*a + (1-a)*x2[i];
            }

            return newX;
        }

        double[] HeuristicCrossover(double[] x1, double[] x2)
        {
            double a;
            double[] newX = new double[x1.Count()];

            a = random.NextDouble();
            for (int i = 0; i < x1.Count(); i++)
            { 
                newX[i] = a*(x2[i]-x1[i]) + x2[i];
            }

            return newX;
        }

        double[] Mutation (double[] x)
        {
            if (random.NextDouble() < pMut)
            {
                for (int i = 0; i < x.Count(); i++)
                {
                    x[i] = random.NextDouble() * (upperBound - (lowerBound)) + (lowerBound);
                }
            }
            return x;

        }

        double[] Mutation_improved(double[] x, IFunction f)
        {
            double r = 1;
            double s = random.NextDouble();
            int b = 5;
            double dg, gg;

            r -= Math.Pow(s, (Math.Pow(1-(f.Eval()/ evaluation), b)));
            dg = Math.Max(lowerBound, (upperBound - lowerBound) * r);
            gg = Math.Min(upperBound, (upperBound - lowerBound) * r);


            if (random.NextDouble() < pMut)
            {
                for (int i = 0; i < x.Count(); i++)
                {
                    x[i] = random.NextDouble() * (gg - (dg)) + (dg);
                }
            }
            return x;

        }


        double[][] MakePopulation(int n)
        {
            double[][] population = new double[N][];

            for (int i = 0; i < N; i++)
            {
                population[i] = new double[n];
                for (int j = 0; j < n; j++)
                {
                    population[i][j] = random.NextDouble() * (upperBound - (lowerBound)) + (lowerBound);
                }
            }

            return population;
        }

        void CalculateFitness (IFunction f, double fxworst, double fxbest)
        {
            for (int i = 0; i < N; i++)
            {
                fi[i] = Fitness(f, population[i], fxworst, fxbest);
            }

        }

        double Fitness (IFunction f, double[] xi, double fxworst, double fxbest, double a=0, double b=1)
        {
            double fi = 0;
            fi = a + (b - a) * (f.Value(xi)-fxworst)/(fxbest-fxworst);
            return fi;
        }
    }
}
