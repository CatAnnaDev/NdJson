using System;
using System.Collections;
using System.Collections.Generic;

namespace NdJson.Tests
{
    public static class Check
    {
        private static readonly List<string> Failures = new List<string>();
        private static string _currentTest;
        private static int _assertions;

        public static int PassedTests;
        public static int FailedTests;

        public static void Run(string name, Action test)
        {
            _currentTest = name;
            int before = Failures.Count;
            try
            {
                test();
            }
            catch (Exception error)
            {
                Failures.Add(name + " : exception non attendue " + error.GetType().Name + " : " + error.Message);
            }

            if (Failures.Count == before)
            {
                PassedTests++;
                Console.WriteLine("  ok   " + name);
            }
            else
            {
                FailedTests++;
                Console.WriteLine("  FAIL " + name);
                for (int i = before; i < Failures.Count; i++)
                {
                    Console.WriteLine("         " + Failures[i]);
                }
            }
        }

        public static void Equal(string expected, string actual, string message)
        {
            _assertions++;
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                Failures.Add(_currentTest + " : " + message + Environment.NewLine + "         attendu : " + expected + Environment.NewLine + "         obtenu  : " + actual);
            }
        }

        public static void Equal<T>(T expected, T actual, string message)
        {
            _assertions++;
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                Failures.Add(_currentTest + " : " + message + " (attendu " + Format(expected) + ", obtenu " + Format(actual) + ")");
            }
        }

        public static void True(bool condition, string message)
        {
            _assertions++;
            if (!condition)
            {
                Failures.Add(_currentTest + " : " + message);
            }
        }

        public static void False(bool condition, string message)
        {
            True(!condition, message);
        }

        public static void Null(object value, string message)
        {
            True(value == null, message);
        }

        public static void NotNull(object value, string message)
        {
            True(value != null, message);
        }

        public static void SequenceEqual<T>(IList<T> expected, IList<T> actual, string message)
        {
            _assertions++;
            if (expected == null || actual == null)
            {
                if (!ReferenceEquals(expected, actual))
                {
                    Failures.Add(_currentTest + " : " + message + " (une des sequences est nulle)");
                }

                return;
            }

            if (expected.Count != actual.Count)
            {
                Failures.Add(_currentTest + " : " + message + " (taille attendue " + expected.Count + ", obtenue " + actual.Count + ")");
                return;
            }

            for (int i = 0; i < expected.Count; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(expected[i], actual[i]))
                {
                    Failures.Add(_currentTest + " : " + message + " (index " + i + " : attendu " + Format(expected[i]) + ", obtenu " + Format(actual[i]) + ")");
                    return;
                }
            }
        }

        public static void Throws<TException>(Action action, string message) where TException : Exception
        {
            _assertions++;
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            catch (Exception other)
            {
                Failures.Add(_currentTest + " : " + message + " (exception " + other.GetType().Name + " au lieu de " + typeof(TException).Name + ")");
                return;
            }

            Failures.Add(_currentTest + " : " + message + " (aucune exception levee)");
        }

        private static string Format(object value)
        {
            if (value == null)
            {
                return "null";
            }

            IFormattable formattable = value as IFormattable;
            if (formattable != null)
            {
                return formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }

        public static int Summary()
        {
            Console.WriteLine();
            Console.WriteLine("Tests : " + (PassedTests + FailedTests) + ", reussis : " + PassedTests + ", echecs : " + FailedTests + ", assertions : " + _assertions);
            return FailedTests == 0 ? 0 : 1;
        }
    }
}
