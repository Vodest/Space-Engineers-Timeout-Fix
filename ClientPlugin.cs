using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace ClientPlugin
{
    public static class MethodUtil
    {
        public static void SwapMethod(MethodBase source, MethodBase dest)
        {
            bool flag = !MethodUtil.MethodSignaturesEqual(source, dest);
            if (flag)
            {
                throw new ArgumentException("The method signatures are not the same.", "source");
            }
            MethodUtil.SwapMethod(MethodUtil.GetMethodAddress(source), dest);
        }

        public unsafe static void SwapMethod(IntPtr srcAdr, MethodBase dest)
        {
            IntPtr methodAddress = MethodUtil.GetMethodAddress(dest);
            bool flag = IntPtr.Size == 8;
            if (flag)
            {
                ulong* ptr = (ulong*)srcAdr.ToPointer();
                ulong* ptr2 = (ulong*)methodAddress.ToPointer();
                ulong num = *ptr;
                *ptr = *ptr2;
                *ptr2 = num;
            }
            else
            {
                uint* ptr3 = (uint*)methodAddress.ToPointer();
                uint* ptr4 = (uint*)srcAdr.ToPointer();
                uint num2 = *ptr4;
                *ptr4 = *ptr3;
                *ptr3 = num2;
            }
        }

        public static void ReplaceMethod(MethodBase source, MethodBase dest)
        {
            bool flag = !MethodUtil.MethodSignaturesEqual(source, dest);
            if (flag)
            {
                throw new ArgumentException("The method signatures are not the same.", "source");
            }
            MethodUtil.ReplaceMethod(MethodUtil.GetMethodAddress(source), dest);
        }

        public unsafe static void ReplaceMethod(IntPtr srcAdr, MethodBase dest)
        {
            IntPtr methodAddress = MethodUtil.GetMethodAddress(dest);
            bool flag = IntPtr.Size == 8;
            if (flag)
            {
                ulong* ptr = (ulong*)methodAddress.ToPointer();
                *ptr = (ulong)(*(long*)srcAdr.ToPointer());
            }
            else
            {
                uint* ptr2 = (uint*)methodAddress.ToPointer();
                *ptr2 = *(uint*)srcAdr.ToPointer();
            }
        }

        public unsafe static IntPtr GetMethodAddress(MethodBase method)
        {
            bool flag = method is DynamicMethod;
            IntPtr intPtr;
            if (flag)
            {
                intPtr = MethodUtil.GetDynamicMethodAddress(method);
            }
            else
            {
                RuntimeHelpers.PrepareMethod(method.MethodHandle);
                intPtr = new IntPtr((void*)((byte*)method.MethodHandle.Value.ToPointer() + 8L));
            }
            return intPtr;
        }

        private unsafe static IntPtr GetDynamicMethodAddress(MethodBase method)
        {
            byte* ptr = (byte*)MethodUtil.GetDynamicMethodRuntimeHandle(method).Value.ToPointer();
            bool flag = IntPtr.Size == 8;
            IntPtr intPtr;
            if (flag)
            {
                ulong* ptr2 = (ulong*)ptr;
                ptr2 += 6;
                intPtr = new IntPtr((void*)ptr2);
            }
            else
            {
                uint* ptr3 = (uint*)ptr;
                ptr3 += 6;
                intPtr = new IntPtr((void*)ptr3);
            }
            return intPtr;
        }

        private static RuntimeMethodHandle GetDynamicMethodRuntimeHandle(MethodBase method)
        {
            bool flag = method is DynamicMethod;
            if (flag)
            {
                FieldInfo field = typeof(DynamicMethod).GetField("m_method", BindingFlags.Instance | BindingFlags.NonPublic);
                bool flag2 = field != null;
                if (flag2)
                {
                    return (RuntimeMethodHandle)field.GetValue(method);
                }
            }
            return method.MethodHandle;
        }

        private static bool MethodSignaturesEqual(MethodBase x, MethodBase y)
        {
            bool flag = x.CallingConvention != y.CallingConvention;
            bool flag2;
            if (flag)
            {
                flag2 = false;
            }
            else
            {
                Type methodReturnType = MethodUtil.GetMethodReturnType(x);
                Type methodReturnType2 = MethodUtil.GetMethodReturnType(y);
                bool flag3 = methodReturnType != methodReturnType2;
                if (flag3)
                {
                    flag2 = false;
                }
                else
                {
                    ParameterInfo[] parameters = x.GetParameters();
                    ParameterInfo[] parameters2 = y.GetParameters();
                    bool flag4 = parameters.Length != parameters2.Length;
                    if (flag4)
                    {
                        flag2 = false;
                    }
                    else
                    {
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            bool flag5 = parameters[i].ParameterType != parameters2[i].ParameterType;
                            if (flag5)
                            {
                                return false;
                            }
                        }
                        flag2 = true;
                    }
                }
            }
            return flag2;
        }

        private static Type GetMethodReturnType(MethodBase method)
        {
            MethodInfo methodInfo = method as MethodInfo;
            bool flag = methodInfo == null;
            if (flag)
            {
                throw new ArgumentException("Unsupported MethodBase : " + method.GetType().Name, "method");
            }
            return methodInfo.ReturnType;
        }
    }
}
