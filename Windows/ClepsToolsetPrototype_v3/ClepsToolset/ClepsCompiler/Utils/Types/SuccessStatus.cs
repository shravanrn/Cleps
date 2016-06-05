using ClepsCompiler.Utils.ClassBehaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClepsCompiler.Utils.Types
{
    class SuccessStatus : EqualsAndHashCode<SuccessStatus>
    {
        private bool IsSuccess;

        private SuccessStatus() { }

        public static SuccessStatus Success = new SuccessStatus() { IsSuccess = true };
        public static SuccessStatus Failure = new SuccessStatus() { IsSuccess = false };

        public override int GetHashCode()
        {
            return IsSuccess.GetHashCode();
        }

        public override bool NotNullObjectEquals(SuccessStatus obj)
        {
            return IsSuccess == obj.IsSuccess;
        }
    }
}
