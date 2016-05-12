using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.Utils.ClassBehaviors
{
    abstract class EqualsAndHashCode<T> where T : class
    {
        public abstract override int GetHashCode();
        public abstract bool NotNullObjectEquals(T obj);

        public int GetHashCodeFor(params object[] fields)
        {
            //Recommended approach in effective java book and java and android docs
            int result = 17;
            foreach (object field in fields)
            {
                if (Object.ReferenceEquals(null, field))
                {
                    result = result * 31 + field.GetHashCode();
                }
            }

            return result;
        }
            
        public override bool Equals(object obj)
        {
            T objToCompare = obj as T;
            if (Object.ReferenceEquals(null, objToCompare))
            {
                return false;
            }

            return NotNullObjectEquals(objToCompare);
        }

        public static bool operator ==(EqualsAndHashCode<T> o1, EqualsAndHashCode<T> o2)
        {
            if (Object.ReferenceEquals(o1, null))
            {
                return Object.ReferenceEquals(o2, null);
            }

            return o1.Equals(o2);
        }

        public static bool operator !=(EqualsAndHashCode<T> o1, EqualsAndHashCode<T> o2)
        {
            return !(o1 == o2);
        }
    }
}
