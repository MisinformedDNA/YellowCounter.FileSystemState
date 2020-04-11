using System;
using System.Collections.Generic;
using System.Text;
using YellowCounter.FileSystemState.HashCodes;

namespace PathReduxTests.HashCodes
{
    public class ControllableHashCode : IHashCode
    {
        private StringBuilder stringBuilder = new StringBuilder();
        private bool dead = false;

        public void Add(char value)
        {
            stringBuilder.Append(value);
        }

        public int ToHashCode()
        {
            deadCheck();

            string arg = stringBuilder.ToString();

            // Use comma as delimiter between desired hash number and remaining text.
            int commaPos = arg.IndexOf(',');

            if(commaPos == -1)
                throw new Exception($"{nameof(ControllableHashCode)} requires , in each string");

            if(int.TryParse(arg.Substring(0, commaPos), out int result))
                return result;

            throw new Exception("Text before , must be an integer");
        }

        private void deadCheck()
        {
            if(dead)
                throw new Exception("Cannot call ToHashCode() twice");

            dead = true;
        }
    }
}
