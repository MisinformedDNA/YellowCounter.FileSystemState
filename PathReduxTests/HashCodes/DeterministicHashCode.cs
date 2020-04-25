using System;
using System.Collections.Generic;
using System.Text;
using YellowCounter.FileSystemState.HashCodes;

namespace PathReduxTests.HashCodes
{
    // Want a deterministic hash function so our tests are repeatable.
    // https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/

    public class DeterministicHashCode : IHashCode
    {
        private bool dead = false;
        private bool odd = false;
        private int hash1 = 352654597; //(5381 << 16) + 5381;
        private int hash2 = 352654597;

        public void Add(char value)
        {
            unchecked
            {
                if(!odd)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ value;

                }
                else
                {
                    hash2 = ((hash2 << 5) + hash2) ^ value;

                }
            }

            odd = !odd;
        }

        public int ToHashCode()
        {
            deadCheck();

            unchecked
            {
                return hash1 + (hash2 * 1566083941);
            }
        }

        private void deadCheck()
        {
            if(dead)
                throw new Exception("Cannot call ToHashCode() twice");

            dead = true;
        }
    }
}
