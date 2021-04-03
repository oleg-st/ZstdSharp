using System;
using System.Runtime.CompilerServices;
using static ZstdSharp.UnsafeHelper;

namespace ZstdSharp
{
    public static unsafe partial class Methods
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static uint ERR_isError(nuint code)
        {
            return (((code > (unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_maxCode))))) ? 1U : 0U);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ZSTD_ErrorCode ERR_getErrorCode(nuint code)
        {
            if ((ERR_isError(code)) == 0)
            {
                return (ZSTD_ErrorCode)(0);
            }

            return (ZSTD_ErrorCode)(0 - code);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static sbyte* ERR_getErrorName(nuint code)
        {
            return ERR_getErrorString(ERR_getErrorCode(code));
        }

        /*-****************************************
        *  Error Strings
        ******************************************/
        public static sbyte* ERR_getErrorString(ZSTD_ErrorCode code)
        {
            sbyte* notErrorCode = GetStringPointer("Unspecified error code");

            switch (code)
            {
                case ZSTD_ErrorCode.ZSTD_error_no_error:
                {
                    return GetStringPointer("No error detected");
                }

                case ZSTD_ErrorCode.ZSTD_error_GENERIC:
                {
                    return GetStringPointer("Error (generic)");
                }

                case ZSTD_ErrorCode.ZSTD_error_prefix_unknown:
                {
                    return GetStringPointer("Unknown frame descriptor");
                }

                case ZSTD_ErrorCode.ZSTD_error_version_unsupported:
                {
                    return GetStringPointer("Version not supported");
                }

                case ZSTD_ErrorCode.ZSTD_error_frameParameter_unsupported:
                {
                    return GetStringPointer("Unsupported frame parameter");
                }

                case ZSTD_ErrorCode.ZSTD_error_frameParameter_windowTooLarge:
                {
                    return GetStringPointer("Frame requires too much memory for decoding");
                }

                case ZSTD_ErrorCode.ZSTD_error_corruption_detected:
                {
                    return GetStringPointer("Corrupted block detected");
                }

                case ZSTD_ErrorCode.ZSTD_error_checksum_wrong:
                {
                    return GetStringPointer("Restored data doesn't match checksum");
                }

                case ZSTD_ErrorCode.ZSTD_error_parameter_unsupported:
                {
                    return GetStringPointer("Unsupported parameter");
                }

                case ZSTD_ErrorCode.ZSTD_error_parameter_outOfBound:
                {
                    return GetStringPointer("Parameter is out of bound");
                }

                case ZSTD_ErrorCode.ZSTD_error_init_missing:
                {
                    return GetStringPointer("Context should be init first");
                }

                case ZSTD_ErrorCode.ZSTD_error_memory_allocation:
                {
                    return GetStringPointer("Allocation error : not enough memory");
                }

                case ZSTD_ErrorCode.ZSTD_error_workSpace_tooSmall:
                {
                    return GetStringPointer("workSpace buffer is not large enough");
                }

                case ZSTD_ErrorCode.ZSTD_error_stage_wrong:
                {
                    return GetStringPointer("Operation not authorized at current processing stage");
                }

                case ZSTD_ErrorCode.ZSTD_error_tableLog_tooLarge:
                {
                    return GetStringPointer("tableLog requires too much memory : unsupported");
                }

                case ZSTD_ErrorCode.ZSTD_error_maxSymbolValue_tooLarge:
                {
                    return GetStringPointer("Unsupported max Symbol Value : too large");
                }

                case ZSTD_ErrorCode.ZSTD_error_maxSymbolValue_tooSmall:
                {
                    return GetStringPointer("Specified maxSymbolValue is too small");
                }

                case ZSTD_ErrorCode.ZSTD_error_dictionary_corrupted:
                {
                    return GetStringPointer("Dictionary is corrupted");
                }

                case ZSTD_ErrorCode.ZSTD_error_dictionary_wrong:
                {
                    return GetStringPointer("Dictionary mismatch");
                }

                case ZSTD_ErrorCode.ZSTD_error_dictionaryCreation_failed:
                {
                    return GetStringPointer("Cannot create Dictionary from provided samples");
                }

                case ZSTD_ErrorCode.ZSTD_error_dstSize_tooSmall:
                {
                    return GetStringPointer("Destination buffer is too small");
                }

                case ZSTD_ErrorCode.ZSTD_error_srcSize_wrong:
                {
                    return GetStringPointer("Src size is incorrect");
                }

                case ZSTD_ErrorCode.ZSTD_error_dstBuffer_null:
                {
                    return GetStringPointer("Operation on NULL destination buffer");
                }

                case ZSTD_ErrorCode.ZSTD_error_frameIndex_tooLarge:
                {
                    return GetStringPointer("Frame index is too large");
                }

                case ZSTD_ErrorCode.ZSTD_error_seekableIO:
                {
                    return GetStringPointer("An I/O error occurred when reading/seeking");
                }

                case ZSTD_ErrorCode.ZSTD_error_dstBuffer_wrong:
                {
                    return GetStringPointer("Destination buffer is wrong");
                }

                case ZSTD_ErrorCode.ZSTD_error_srcBuffer_wrong:
                {
                    return GetStringPointer("Source buffer is wrong");
                }

                case ZSTD_ErrorCode.ZSTD_error_maxCode:
                default:
                {
                    return notErrorCode;
                }
            }
        }
    }
}
