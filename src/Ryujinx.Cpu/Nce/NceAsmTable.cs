using System;

namespace Ryujinx.Cpu.Nce
{
    static class NceAsmTable
    {
        public static uint[] GetTpidrEl0Code = new uint[]
        {
            GetMrsTpidrEl0(0), // mrs x0, tpidr_el0
            0xd65f03c0u, // ret
        };

        public static uint[] ThreadStartCode = new uint[]
        {
            0xa9ae53f3u, // stp x19, x20, [sp, #-288]!
            0xa9015bf5u, // stp x21, x22, [sp, #16]
            0xa90263f7u, // stp x23, x24, [sp, #32]
            0xa9036bf9u, // stp x25, x26, [sp, #48]
            0xa90473fbu, // stp x27, x28, [sp, #64]
            0xa9057bfdu, // stp x29, x30, [sp, #80]
            0x6d0627e8u, // stp d8, d9, [sp, #96]
            0x6d072feau, // stp d10, d11, [sp, #112]
            0x6d0837ecu, // stp d12, d13, [sp, #128]
            0x6d093feeu, // stp d14, d15, [sp, #144]
            0x6d0a47f0u, // stp d16, d17, [sp, #160]
            0x6d0b4ff2u, // stp d18, d19, [sp, #176]
            0x6d0c57f4u, // stp d20, d21, [sp, #192]
            0x6d0d5ff6u, // stp d22, d23, [sp, #208]
            0x6d0e67f8u, // stp d24, d25, [sp, #224]
            0x6d0f6ffau, // stp d26, d27, [sp, #240]
            0x6d1077fcu, // stp d28, d29, [sp, #256]
            0x6d117ffeu, // stp d30, d31, [sp, #272]
            0xb9031c1fu, // str wzr, [x0, #796]
            0x910003e1u, // mov x1, sp
            0xf9019001u, // str x1, [x0, #800]
            0xa9410c02u, // ldp x2, x3, [x0, #16]
            0xa9421404u, // ldp x4, x5, [x0, #32]
            0xa9431c06u, // ldp x6, x7, [x0, #48]
            0xa9442408u, // ldp x8, x9, [x0, #64]
            0xa9452c0au, // ldp x10, x11, [x0, #80]
            0xa946340cu, // ldp x12, x13, [x0, #96]
            0xa9473c0eu, // ldp x14, x15, [x0, #112]
            0xa9484410u, // ldp x16, x17, [x0, #128]
            0xa9494c12u, // ldp x18, x19, [x0, #144]
            0xa94a5414u, // ldp x20, x21, [x0, #160]
            0xa94b5c16u, // ldp x22, x23, [x0, #176]
            0xa94c6418u, // ldp x24, x25, [x0, #192]
            0xa94d6c1au, // ldp x26, x27, [x0, #208]
            0xa94e741cu, // ldp x28, x29, [x0, #224]
            0xad480400u, // ldp q0, q1, [x0, #256]
            0xad490c02u, // ldp q2, q3, [x0, #288]
            0xad4a1404u, // ldp q4, q5, [x0, #320]
            0xad4b1c06u, // ldp q6, q7, [x0, #352]
            0xad4c2408u, // ldp q8, q9, [x0, #384]
            0xad4d2c0au, // ldp q10, q11, [x0, #416]
            0xad4e340cu, // ldp q12, q13, [x0, #448]
            0xad4f3c0eu, // ldp q14, q15, [x0, #480]
            0xad504410u, // ldp q16, q17, [x0, #512]
            0xad514c12u, // ldp q18, q19, [x0, #544]
            0xad525414u, // ldp q20, q21, [x0, #576]
            0xad535c16u, // ldp q22, q23, [x0, #608]
            0xad546418u, // ldp q24, q25, [x0, #640]
            0xad556c1au, // ldp q26, q27, [x0, #672]
            0xad56741cu, // ldp q28, q29, [x0, #704]
            0xad577c1eu, // ldp q30, q31, [x0, #736]
            0xa94f041eu, // ldp x30, x1, [x0, #240]
            0x9100003fu, // mov sp, x1
            0xa9400400u, // ldp x0, x1, [x0]
            0xd61f03c0u, // br x30
        };

        public static uint[] ExceptionHandlerEntryCode = new uint[]
        {
            0xa9bc53f3u, // stp x19, x20, [sp, #-64]!
            0xa9015bf5u, // stp x21, x22, [sp, #16]
            0xa90263f7u, // stp x23, x24, [sp, #32]
            0xf9001bf9u, // str x25, [sp, #48]
            0xaa0003f3u, // mov x19, x0
            0xaa0103f4u, // mov x20, x1
            0xaa0203f5u, // mov x21, x2
            0x910003f6u, // mov x22, sp
            0xaa1e03f7u, // mov x23, x30
            0xd2800018u, // mov x24, #0x0
            0xf2a00018u, // movk x24, #0x0, lsl #16
            0xf2c00018u, // movk x24, #0x0, lsl #32
            0xf2e00018u, // movk x24, #0x0, lsl #48
            0xf85f8319u, // ldur x25, [x24, #-8]
            0x8b191319u, // add x25, x24, x25, lsl #4
            GetMrsTpidrEl0(1), // mrs x1, tpidr_el0
            0xeb19031fu, // cmp x24, x25
            0x540000a0u, // b.eq 13c <ExceptionHandlerEntryCode+0x58>
            0xf8410702u, // ldr x2, [x24], #16
            0xeb02003fu, // cmp x1, x2
            0x54000080u, // b.eq 144 <ExceptionHandlerEntryCode+0x60>
            0x17fffffbu, // b 124 <ExceptionHandlerEntryCode+0x40>
            0xd2800018u, // mov x24, #0x0
            0x14000002u, // b 148 <ExceptionHandlerEntryCode+0x64>
            0xf85f8318u, // ldur x24, [x24, #-8]
            0xb4000438u, // cbz x24, 1cc <ExceptionHandlerEntryCode+0xe8>
            0xf9419300u, // ldr x0, [x24, #800]
            0x9100001fu, // mov sp, x0
            0x7100027fu, // cmp w19, #0x0
            0x54000180u, // b.eq 188 <ExceptionHandlerEntryCode+0xa4>
            0x52800020u, // mov w0, #0x1
            0xb9031f00u, // str w0, [x24, #796]
            0xaa1303e0u, // mov x0, x19
            0xaa1403e1u, // mov x1, x20
            0xaa1503e2u, // mov x2, x21
            0xd2800008u, // mov x8, #0x0
            0xf2a00008u, // movk x8, #0x0, lsl #16
            0xf2c00008u, // movk x8, #0x0, lsl #32
            0xf2e00008u, // movk x8, #0x0, lsl #48
            0xd63f0100u, // blr x8
            0x1400000au, // b 1ac <ExceptionHandlerEntryCode+0xc8>
            0xb9431f00u, // ldr w0, [x24, #796]
            0x35000120u, // cbnz w0, 1b0 <ExceptionHandlerEntryCode+0xcc>
            0x52800020u, // mov w0, #0x1
            0xb9031f00u, // str w0, [x24, #796]
            0xd2800000u, // mov x0, #0x0
            0xf2a00000u, // movk x0, #0x0, lsl #16
            0xf2c00000u, // movk x0, #0x0, lsl #32
            0xf2e00000u, // movk x0, #0x0, lsl #48
            0xd63f0000u, // blr x0
            0xb9031f1fu, // str wzr, [x24, #796]
            0x910002dfu, // mov sp, x22
            0xaa1703feu, // mov x30, x23
            0xa9415bf5u, // ldp x21, x22, [sp, #16]
            0xa94263f7u, // ldp x23, x24, [sp, #32]
            0xa9436bf9u, // ldp x25, x26, [sp, #48]
            0xa8c453f3u, // ldp x19, x20, [sp], #64
            0xd65f03c0u, // ret
            0xaa1303e0u, // mov x0, x19
            0xaa1403e1u, // mov x1, x20
            0xaa1503e2u, // mov x2, x21
            0x910002dfu, // mov sp, x22
            0xa9415bf5u, // ldp x21, x22, [sp, #16]
            0xa94263f7u, // ldp x23, x24, [sp, #32]
            0xf9401bf9u, // ldr x25, [sp, #48]
            0xa8c453f3u, // ldp x19, x20, [sp], #64
            0xd2800003u, // mov x3, #0x0
            0xf2a00003u, // movk x3, #0x0, lsl #16
            0xf2c00003u, // movk x3, #0x0, lsl #32
            0xf2e00003u, // movk x3, #0x0, lsl #48
            0xd61f0060u, // br x3
        };

        public static uint[] SvcPatchCode = new uint[]
        {
            0xa9be53f3u, // stp x19, x20, [sp, #-32]!
            0xf9000bf5u, // str x21, [sp, #16]
            0xd2800013u, // mov x19, #0x0
            0xf2a00013u, // movk x19, #0x0, lsl #16
            0xf2c00013u, // movk x19, #0x0, lsl #32
            0xf2e00013u, // movk x19, #0x0, lsl #48
            GetMrsTpidrEl0(20), // mrs x20, tpidr_el0
            0xf8410675u, // ldr x21, [x19], #16
            0xeb15029fu, // cmp x20, x21
            0x54000040u, // b.eq 22c <SvcPatchCode+0x2c>
            0x17fffffdu, // b 21c <SvcPatchCode+0x1c>
            0xf85f8273u, // ldur x19, [x19, #-8]
            0xa9000660u, // stp x0, x1, [x19]
            0xa9010e62u, // stp x2, x3, [x19, #16]
            0xa9021664u, // stp x4, x5, [x19, #32]
            0xa9031e66u, // stp x6, x7, [x19, #48]
            0xa9042668u, // stp x8, x9, [x19, #64]
            0xa9052e6au, // stp x10, x11, [x19, #80]
            0xa906366cu, // stp x12, x13, [x19, #96]
            0xa9073e6eu, // stp x14, x15, [x19, #112]
            0xa9084670u, // stp x16, x17, [x19, #128]
            0xf9400bf5u, // ldr x21, [sp, #16]
            0xa8c253e0u, // ldp x0, x20, [sp], #32
            0xa9090272u, // stp x18, x0, [x19, #144]
            0xa90a5674u, // stp x20, x21, [x19, #160]
            0xa90b5e76u, // stp x22, x23, [x19, #176]
            0xa90c6678u, // stp x24, x25, [x19, #192]
            0xa90d6e7au, // stp x26, x27, [x19, #208]
            0xa90e767cu, // stp x28, x29, [x19, #224]
            0x910003e0u, // mov x0, sp
            0xa90f027eu, // stp x30, x0, [x19, #240]
            0xad080660u, // stp q0, q1, [x19, #256]
            0xad090e62u, // stp q2, q3, [x19, #288]
            0xad0a1664u, // stp q4, q5, [x19, #320]
            0xad0b1e66u, // stp q6, q7, [x19, #352]
            0xad0c2668u, // stp q8, q9, [x19, #384]
            0xad0d2e6au, // stp q10, q11, [x19, #416]
            0xad0e366cu, // stp q12, q13, [x19, #448]
            0xad0f3e6eu, // stp q14, q15, [x19, #480]
            0xad104670u, // stp q16, q17, [x19, #512]
            0xad114e72u, // stp q18, q19, [x19, #544]
            0xad125674u, // stp q20, q21, [x19, #576]
            0xad135e76u, // stp q22, q23, [x19, #608]
            0xad146678u, // stp q24, q25, [x19, #640]
            0xad156e7au, // stp q26, q27, [x19, #672]
            0xad16767cu, // stp q28, q29, [x19, #704]
            0xad177e7eu, // stp q30, q31, [x19, #736]
            0xf9419260u, // ldr x0, [x19, #800]
            0x9100001fu, // mov sp, x0
            0x52800020u, // mov w0, #0x1
            0xb9031e60u, // str w0, [x19, #796]
            0x52800000u, // mov w0, #0x0
            0xf941aa68u, // ldr x8, [x19, #848]
            0xd63f0100u, // blr x8
            0x35000280u, // cbnz w0, 328 <SvcPatchCode+0x128>
            0x6d517ffeu, // ldp d30, d31, [sp, #272]
            0x6d5077fcu, // ldp d28, d29, [sp, #256]
            0x6d4f6ffau, // ldp d26, d27, [sp, #240]
            0x6d4e67f8u, // ldp d24, d25, [sp, #224]
            0x6d4d5ff6u, // ldp d22, d23, [sp, #208]
            0x6d4c57f4u, // ldp d20, d21, [sp, #192]
            0x6d4b4ff2u, // ldp d18, d19, [sp, #176]
            0x6d4a47f0u, // ldp d16, d17, [sp, #160]
            0x6d493feeu, // ldp d14, d15, [sp, #144]
            0x6d4837ecu, // ldp d12, d13, [sp, #128]
            0x6d472feau, // ldp d10, d11, [sp, #112]
            0x6d4627e8u, // ldp d8, d9, [sp, #96]
            0xa9457bfdu, // ldp x29, x30, [sp, #80]
            0xa94473fbu, // ldp x27, x28, [sp, #64]
            0xa9436bf9u, // ldp x25, x26, [sp, #48]
            0xa94263f7u, // ldp x23, x24, [sp, #32]
            0xa9415bf5u, // ldp x21, x22, [sp, #16]
            0xa8d253f3u, // ldp x19, x20, [sp], #288
            0xd65f03c0u, // ret
            0xb9031e7fu, // str wzr, [x19, #796]
            0xa94f027eu, // ldp x30, x0, [x19, #240]
            0x9100001fu, // mov sp, x0
            0xa9400660u, // ldp x0, x1, [x19]
            0xa9410e62u, // ldp x2, x3, [x19, #16]
            0xa9421664u, // ldp x4, x5, [x19, #32]
            0xa9431e66u, // ldp x6, x7, [x19, #48]
            0xa9442668u, // ldp x8, x9, [x19, #64]
            0xa9452e6au, // ldp x10, x11, [x19, #80]
            0xa946366cu, // ldp x12, x13, [x19, #96]
            0xa9473e6eu, // ldp x14, x15, [x19, #112]
            0xa9484670u, // ldp x16, x17, [x19, #128]
            0xf9404a72u, // ldr x18, [x19, #144]
            0xa94a5674u, // ldp x20, x21, [x19, #160]
            0xa94b5e76u, // ldp x22, x23, [x19, #176]
            0xa94c6678u, // ldp x24, x25, [x19, #192]
            0xa94d6e7au, // ldp x26, x27, [x19, #208]
            0xa94e767cu, // ldp x28, x29, [x19, #224]
            0xad480660u, // ldp q0, q1, [x19, #256]
            0xad490e62u, // ldp q2, q3, [x19, #288]
            0xad4a1664u, // ldp q4, q5, [x19, #320]
            0xad4b1e66u, // ldp q6, q7, [x19, #352]
            0xad4c2668u, // ldp q8, q9, [x19, #384]
            0xad4d2e6au, // ldp q10, q11, [x19, #416]
            0xad4e366cu, // ldp q12, q13, [x19, #448]
            0xad4f3e6eu, // ldp q14, q15, [x19, #480]
            0xad504670u, // ldp q16, q17, [x19, #512]
            0xad514e72u, // ldp q18, q19, [x19, #544]
            0xad525674u, // ldp q20, q21, [x19, #576]
            0xad535e76u, // ldp q22, q23, [x19, #608]
            0xad546678u, // ldp q24, q25, [x19, #640]
            0xad556e7au, // ldp q26, q27, [x19, #672]
            0xad56767cu, // ldp q28, q29, [x19, #704]
            0xad577e7eu, // ldp q30, q31, [x19, #736]
            0xf9404e73u, // ldr x19, [x19, #152]
            0x14000000u, // b 3b4 <SvcPatchCode+0x1b4>
        };

        public static uint[] MrsTpidrroEl0PatchCode = new uint[]
        {
            0xa9be4fffu, // stp xzr, x19, [sp, #-32]!
            0xa90157f4u, // stp x20, x21, [sp, #16]
            0xd2800013u, // mov x19, #0x0
            0xf2a00013u, // movk x19, #0x0, lsl #16
            0xf2c00013u, // movk x19, #0x0, lsl #32
            0xf2e00013u, // movk x19, #0x0, lsl #48
            GetMrsTpidrEl0(20), // mrs x20, tpidr_el0
            0xf8410675u, // ldr x21, [x19], #16
            0xeb15029fu, // cmp x20, x21
            0x54000040u, // b.eq 3e4 <MrsTpidrroEl0PatchCode+0x2c>
            0x17fffffdu, // b 3d4 <MrsTpidrroEl0PatchCode+0x1c>
            0xf85f8273u, // ldur x19, [x19, #-8]
            0xf9418673u, // ldr x19, [x19, #776]
            0xf90003f3u, // str x19, [sp]
            0xa94157f4u, // ldp x20, x21, [sp, #16]
            0xf94007f3u, // ldr x19, [sp, #8]
            0xf84207e0u, // ldr x0, [sp], #32
            0x14000000u, // b 3fc <MrsTpidrroEl0PatchCode+0x44>
        };

        public static uint[] MrsTpidrEl0PatchCode = new uint[]
        {
            0xa9be4fffu, // stp xzr, x19, [sp, #-32]!
            0xa90157f4u, // stp x20, x21, [sp, #16]
            0xd2800013u, // mov x19, #0x0
            0xf2a00013u, // movk x19, #0x0, lsl #16
            0xf2c00013u, // movk x19, #0x0, lsl #32
            0xf2e00013u, // movk x19, #0x0, lsl #48
            GetMrsTpidrEl0(20), // mrs x20, tpidr_el0
            0xf8410675u, // ldr x21, [x19], #16
            0xeb15029fu, // cmp x20, x21
            0x54000040u, // b.eq 42c <MrsTpidrEl0PatchCode+0x2c>
            0x17fffffdu, // b 41c <MrsTpidrEl0PatchCode+0x1c>
            0xf85f8273u, // ldur x19, [x19, #-8]
            0xf9418273u, // ldr x19, [x19, #768]
            0xf90003f3u, // str x19, [sp]
            0xa94157f4u, // ldp x20, x21, [sp, #16]
            0xf94007f3u, // ldr x19, [sp, #8]
            0xf84207e0u, // ldr x0, [sp], #32
            0x14000000u, // b 444 <MrsTpidrEl0PatchCode+0x44>
        };

        public static uint[] MrsCtrEl0PatchCode = new uint[]
        {
            0xa9be4fffu, // stp xzr, x19, [sp, #-32]!
            0xa90157f4u, // stp x20, x21, [sp, #16]
            0xd2800013u, // mov x19, #0x0
            0xf2a00013u, // movk x19, #0x0, lsl #16
            0xf2c00013u, // movk x19, #0x0, lsl #32
            0xf2e00013u, // movk x19, #0x0, lsl #48
            GetMrsTpidrEl0(20), // mrs x20, tpidr_el0
            0xf8410675u, // ldr x21, [x19], #16
            0xeb15029fu, // cmp x20, x21
            0x54000040u, // b.eq 474 <MrsCtrEl0PatchCode+0x2c>
            0x17fffffdu, // b 464 <MrsCtrEl0PatchCode+0x1c>
            0xf85f8273u, // ldur x19, [x19, #-8]
            0xf9419e73u, // ldr x19, [x19, #824]
            0xf90003f3u, // str x19, [sp]
            0xa94157f4u, // ldp x20, x21, [sp, #16]
            0xf94007f3u, // ldr x19, [sp, #8]
            0xf84207e0u, // ldr x0, [sp], #32
            0x14000000u, // b 48c <MrsCtrEl0PatchCode+0x44>
        };

        public static uint[] MsrTpidrEl0PatchCode = new uint[]
        {
            0xa9be03f3u, // stp x19, x0, [sp, #-32]!
            0xa90157f4u, // stp x20, x21, [sp, #16]
            0xd2800013u, // mov x19, #0x0
            0xf2a00013u, // movk x19, #0x0, lsl #16
            0xf2c00013u, // movk x19, #0x0, lsl #32
            0xf2e00013u, // movk x19, #0x0, lsl #48
            GetMrsTpidrEl0(20), // mrs x20, tpidr_el0
            0xf8410675u, // ldr x21, [x19], #16
            0xeb15029fu, // cmp x20, x21
            0x54000040u, // b.eq 4bc <MsrTpidrEl0PatchCode+0x2c>
            0x17fffffdu, // b 4ac <MsrTpidrEl0PatchCode+0x1c>
            0xf85f8273u, // ldur x19, [x19, #-8]
            0xf94007f4u, // ldr x20, [sp, #8]
            0xf9018274u, // str x20, [x19, #768]
            0xa94157f4u, // ldp x20, x21, [sp, #16]
            0xf84207f3u, // ldr x19, [sp], #32
            0x14000000u, // b 4d0 <MsrTpidrEl0PatchCode+0x40>
        };

        public static uint[] MrsCntpctEl0PatchCode = new uint[]
        {
            0xa9b407e0u, // stp x0, x1, [sp, #-192]!
            0xa9010fe2u, // stp x2, x3, [sp, #16]
            0xa90217e4u, // stp x4, x5, [sp, #32]
            0xa9031fe6u, // stp x6, x7, [sp, #48]
            0xa90427e8u, // stp x8, x9, [sp, #64]
            0xa9052feau, // stp x10, x11, [sp, #80]
            0xa90637ecu, // stp x12, x13, [sp, #96]
            0xa9073feeu, // stp x14, x15, [sp, #112]
            0xa90847f0u, // stp x16, x17, [sp, #128]
            0xa9094ff2u, // stp x18, x19, [sp, #144]
            0xa90a57f4u, // stp x20, x21, [sp, #160]
            0xf9005ffeu, // str x30, [sp, #184]
            0xd2800013u, // mov x19, #0x0
            0xf2a00013u, // movk x19, #0x0, lsl #16
            0xf2c00013u, // movk x19, #0x0, lsl #32
            0xf2e00013u, // movk x19, #0x0, lsl #48
            GetMrsTpidrEl0(20), // mrs x20, tpidr_el0
            0xf8410675u, // ldr x21, [x19], #16
            0xeb15029fu, // cmp x20, x21
            0x54000040u, // b.eq 528 <MrsCntpctEl0PatchCode+0x54>
            0x17fffffdu, // b 518 <MrsCntpctEl0PatchCode+0x44>
            0xf85f8273u, // ldur x19, [x19, #-8]
            0x52800020u, // mov w0, #0x1
            0xb9031e60u, // str w0, [x19, #796]
            0xd2800000u, // mov x0, #0x0
            0xf2a00000u, // movk x0, #0x0, lsl #16
            0xf2c00000u, // movk x0, #0x0, lsl #32
            0xf2e00000u, // movk x0, #0x0, lsl #48
            0xd63f0000u, // blr x0
            0xb9031e7fu, // str wzr, [x19, #796]
            0xf9005be0u, // str x0, [sp, #176]
            0xf9405ffeu, // ldr x30, [sp, #184]
            0xa94a57f4u, // ldp x20, x21, [sp, #160]
            0xa9494ff2u, // ldp x18, x19, [sp, #144]
            0xa94847f0u, // ldp x16, x17, [sp, #128]
            0xa9473feeu, // ldp x14, x15, [sp, #112]
            0xa94637ecu, // ldp x12, x13, [sp, #96]
            0xa9452feau, // ldp x10, x11, [sp, #80]
            0xa94427e8u, // ldp x8, x9, [sp, #64]
            0xa9431fe6u, // ldp x6, x7, [sp, #48]
            0xa94217e4u, // ldp x4, x5, [sp, #32]
            0xa9410fe2u, // ldp x2, x3, [sp, #16]
            0xa8cb07e0u, // ldp x0, x1, [sp], #176
            0xf84107e0u, // ldr x0, [sp], #16
            0x14000000u, // b 584 <MrsCntpctEl0PatchCode+0xb0>
        };

        private static uint GetMrsTpidrEl0(uint rd)
        {
            if (OperatingSystem.IsMacOS())
            {
                return 0xd53bd060u | rd; // TPIDRRO
            }
            else
            {
                return 0xd53bd040u | rd; // TPIDR
            }
        }
    }
}