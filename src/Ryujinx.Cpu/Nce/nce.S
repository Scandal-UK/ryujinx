.text

.macro longmov0 reg
mov \reg, #0
movk \reg, #0, lsl #16
movk \reg, #0, lsl #32
movk \reg, #0, lsl #48
.endm

// r1 = EntriesPointer
// r2 = current_thread_id_local
// r3 = expected_thread_id
// r4 = ThreadsCount_local
.macro loadctxptr_reg r1, r2, r3
longmov0 \r1
mrs \r2, tpidr_el0
1:
ldr \r3, [\r1], #16
cmp \r2, \r3
beq 2f
b 1b
2:
ldr \r1, [\r1, #-8]
.endm

.macro loadctxptr
loadctxptr_reg x19, x20, x21
.endm

.global GetTpidrEl0Code
GetTpidrEl0Code:
mrs x0, tpidr_el0
ret

.global ThreadStartCode
ThreadStartCode:
stp x19, x20, [sp, #-0x120]!
stp x21, x22, [sp, #0x10]
stp x23, x24, [sp, #0x20]
stp x25, x26, [sp, #0x30]
stp x27, x28, [sp, #0x40]
stp x29, x30, [sp, #0x50]
stp d8, d9, [sp, #0x60]
stp d10, d11, [sp, #0x70]
stp d12, d13, [sp, #0x80]
stp d14, d15, [sp, #0x90]
stp d16, d17, [sp, #0xA0]
stp d18, d19, [sp, #0xB0]
stp d20, d21, [sp, #0xC0]
stp d22, d23, [sp, #0xD0]
stp d24, d25, [sp, #0xE0]
stp d26, d27, [sp, #0xF0]
stp d28, d29, [sp, #0x100]
stp d30, d31, [sp, #0x110]
str wzr, [x0, #0x31C]
mov x1, sp
str x1, [x0, #0x320]
ldp x2, x3, [x0, #0x10]
ldp x4, x5, [x0, #0x20]
ldp x6, x7, [x0, #0x30]
ldp x8, x9, [x0, #0x40]
ldp x10, x11, [x0, #0x50]
ldp x12, x13, [x0, #0x60]
ldp x14, x15, [x0, #0x70]
ldp x16, x17, [x0, #0x80]
ldp x18, x19, [x0, #0x90]
ldp x20, x21, [x0, #0xA0]
ldp x22, x23, [x0, #0xB0]
ldp x24, x25, [x0, #0xC0]
ldp x26, x27, [x0, #0xD0]
ldp x28, x29, [x0, #0xE0]
ldp q0, q1, [x0, #0x100]
ldp q2, q3, [x0, #0x120]
ldp q4, q5, [x0, #0x140]
ldp q6, q7, [x0, #0x160]
ldp q8, q9, [x0, #0x180]
ldp q10, q11, [x0, #0x1A0]
ldp q12, q13, [x0, #0x1C0]
ldp q14, q15, [x0, #0x1E0]
ldp q16, q17, [x0, #0x200]
ldp q18, q19, [x0, #0x220]
ldp q20, q21, [x0, #0x240]
ldp q22, q23, [x0, #0x260]
ldp q24, q25, [x0, #0x280]
ldp q26, q27, [x0, #0x2A0]
ldp q28, q29, [x0, #0x2C0]
ldp q30, q31, [x0, #0x2E0]
ldp x30, x1, [x0, #0xF0]
mov sp, x1
ldp x0, x1, [x0, #0x0]
br x30

// Inputs
// r1 = EntriesPointer
// r2 = current_thread_id_local
// r3 = expected_thread_id
// r4 = EntriesPointerEnd

// Outputs
// r1 = EntryPointer or 0x0 on not found
.macro loadctxptr_safe_reg r1, r2, r3, r4
longmov0 \r1
ldr \r4, [\r1, #-8]
add \r4, \r1, \r4, lsl #4
mrs \r2, tpidr_el0
1:
cmp \r1, \r4
beq 2f
ldr \r3, [\r1], #16
cmp \r2, \r3
beq 3f
b 1b
2:
mov \r1, 0x0
b 4f
3:
ldr \r1, [\r1, #-8]
4:
.endm

.global ExceptionHandlerEntryCode
ExceptionHandlerEntryCode:
stp x19, x20, [sp, #-0x40]!
stp x21, x22, [sp, #0x10]
stp x23, x24, [sp, #0x20]
str x25, [sp, #0x30]
// signo
mov x19, x0
// siginfo_t *si
mov x20, x1
// void *thread_id
mov x21, x2
mov x22, sp
mov x23, x30
// x24 = EntriesPointer
// x1 = si
// x2 = thread_id
loadctxptr_safe_reg x24, x1, x2, x25
cbz x24, 4f
ldr x0, [x24, 0x320]
mov sp, x0
cmp w19, #0
beq 1f
mov w0, #1
str w0, [x24, 0x31C]
mov x0, x19
mov x1, x20
mov x2, x21
mov x8, #0
movk x8, #0, lsl #16
movk x8, #0, lsl #32
movk x8, #0, lsl #48
blr x8
b 2f
1:
ldr w0, [x24, 0x31C]
cbnz w0, 3f
mov w0, #1
str w0, [x24, 0x31C]
mov x0, #0
movk x0, #0, lsl #16
movk x0, #0, lsl #32
movk x0, #0, lsl #48
blr x0
2:
str wzr, [x24, 0x31C]
3:
mov sp, x22
mov x30, x23
ldp x21, x22, [sp, #0x10]
ldp x23, x24, [sp, #0x20]
ldp x25, x26, [sp, #0x30]
ldp x19, x20, [sp], #0x40
ret
4:
// ThreadId is invalid, forward to other handler.
mov x0, x19
mov x1, x20
mov x2, x21
mov sp, x22
ldp x21, x22, [sp, #0x10]
ldp x23, x24, [sp, #0x20]
ldr x25, [sp, #0x30]
ldp x19, x20, [sp], #0x40
longmov0 x3
br x3

.global SvcPatchCode
SvcPatchCode:

stp x19, x20, [sp, #-0x20]!
str x21, [sp, #0x10]
loadctxptr
stp x0, x1, [x19, #0x0]
stp x2, x3, [x19, #0x10]
stp x4, x5, [x19, #0x20]
stp x6, x7, [x19, #0x30]
stp x8, x9, [x19, #0x40]
stp x10, x11, [x19, #0x50]
stp x12, x13, [x19, #0x60]
stp x14, x15, [x19, #0x70]
stp x16, x17, [x19, #0x80]
ldr x21, [sp, #0x10]
ldp x0, x20, [sp], #0x20
stp x18, x0, [x19, #0x90]
stp x20, x21, [x19, #0xA0]
stp x22, x23, [x19, #0xB0]
stp x24, x25, [x19, #0xC0]
stp x26, x27, [x19, #0xD0]
stp x28, x29, [x19, #0xE0]
mov x0, sp
stp x30, x0, [x19, #0xF0]
stp q0, q1, [x19, #0x100]
stp q2, q3, [x19, #0x120]
stp q4, q5, [x19, #0x140]
stp q6, q7, [x19, #0x160]
stp q8, q9, [x19, #0x180]
stp q10, q11, [x19, #0x1A0]
stp q12, q13, [x19, #0x1C0]
stp q14, q15, [x19, #0x1E0]
stp q16, q17, [x19, #0x200]
stp q18, q19, [x19, #0x220]
stp q20, q21, [x19, #0x240]
stp q22, q23, [x19, #0x260]
stp q24, q25, [x19, #0x280]
stp q26, q27, [x19, #0x2A0]
stp q28, q29, [x19, #0x2C0]
stp q30, q31, [x19, #0x2E0]
ldr x0, [x19, #0x320]
mov sp, x0
mov w0, #1
str w0, [x19, #0x31C]
mov w0, #0
ldr x8, [x19, #0x350]
blr x8
cbnz w0, 1f
ldp d30, d31, [sp, #0x110]
ldp d28, d29, [sp, #0x100]
ldp d26, d27, [sp, #0xF0]
ldp d24, d25, [sp, #0xE0]
ldp d22, d23, [sp, #0xD0]
ldp d20, d21, [sp, #0xC0]
ldp d18, d19, [sp, #0xB0]
ldp d16, d17, [sp, #0xA0]
ldp d14, d15, [sp, #0x90]
ldp d12, d13, [sp, #0x80]
ldp d10, d11, [sp, #0x70]
ldp d8, d9, [sp, #0x60]
ldp x29, x30, [sp, #0x50]
ldp x27, x28, [sp, #0x40]
ldp x25, x26, [sp, #0x30]
ldp x23, x24, [sp, #0x20]
ldp x21, x22, [sp, #0x10]
ldp x19, x20, [sp], #0x120
ret
1:
str wzr, [x19, #0x31C]
ldp x30, x0, [x19, #0xF0]
mov sp, x0
ldp x0, x1, [x19, #0x0]
ldp x2, x3, [x19, #0x10]
ldp x4, x5, [x19, #0x20]
ldp x6, x7, [x19, #0x30]
ldp x8, x9, [x19, #0x40]
ldp x10, x11, [x19, #0x50]
ldp x12, x13, [x19, #0x60]
ldp x14, x15, [x19, #0x70]
ldp x16, x17, [x19, #0x80]
ldr x18, [x19, #0x90]
ldp x20, x21, [x19, #0xA0]
ldp x22, x23, [x19, #0xB0]
ldp x24, x25, [x19, #0xC0]
ldp x26, x27, [x19, #0xD0]
ldp x28, x29, [x19, #0xE0]
ldp q0, q1, [x19, #0x100]
ldp q2, q3, [x19, #0x120]
ldp q4, q5, [x19, #0x140]
ldp q6, q7, [x19, #0x160]
ldp q8, q9, [x19, #0x180]
ldp q10, q11, [x19, #0x1A0]
ldp q12, q13, [x19, #0x1C0]
ldp q14, q15, [x19, #0x1E0]
ldp q16, q17, [x19, #0x200]
ldp q18, q19, [x19, #0x220]
ldp q20, q21, [x19, #0x240]
ldp q22, q23, [x19, #0x260]
ldp q24, q25, [x19, #0x280]
ldp q26, q27, [x19, #0x2A0]
ldp q28, q29, [x19, #0x2C0]
ldp q30, q31, [x19, #0x2E0]
ldr x19, [x19, #0x98]
b #0

.global MrsTpidrroEl0PatchCode
MrsTpidrroEl0PatchCode:
stp xzr, x19, [sp, #-0x20]!
stp x20, x21, [sp, #0x10]
loadctxptr
ldr x19, [x19, #0x308]
str x19, [sp]
ldp x20, x21, [sp, #0x10]
ldr x19, [sp, #8]
ldr x0, [sp], #0x20
b #0

.global MrsTpidrEl0PatchCode
MrsTpidrEl0PatchCode:
stp xzr, x19, [sp, #-0x20]!
stp x20, x21, [sp, #0x10]
loadctxptr
ldr x19, [x19, #0x300]
str x19, [sp]
ldp x20, x21, [sp, #0x10]
ldr x19, [sp, #8]
ldr x0, [sp], #0x20
b #0

.global MrsCtrEl0PatchCode
MrsCtrEl0PatchCode:
stp xzr, x19, [sp, #-0x20]!
stp x20, x21, [sp, #0x10]
loadctxptr
ldr x19, [x19, #0x338]
str x19, [sp]
ldp x20, x21, [sp, #0x10]
ldr x19, [sp, #8]
ldr x0, [sp], #0x20
b #0

.global MsrTpidrEl0PatchCode
MsrTpidrEl0PatchCode:
stp x19, x0, [sp, #-0x20]!
stp x20, x21, [sp, #0x10]
loadctxptr
ldr x20, [sp, #8]
str x20, [x19, #0x300]
ldp x20, x21, [sp, #0x10]
ldr x19, [sp], #0x20
b #0

.global MrsCntpctEl0PatchCode
MrsCntpctEl0PatchCode:
stp x0, x1, [sp, #-0xC0]!
stp x2, x3, [sp, #0x10]
stp x4, x5, [sp, #0x20]
stp x6, x7, [sp, #0x30]
stp x8, x9, [sp, #0x40]
stp x10, x11, [sp, #0x50]
stp x12, x13, [sp, #0x60]
stp x14, x15, [sp, #0x70]
stp x16, x17, [sp, #0x80]
stp x18, x19, [sp, #0x90]
stp x20, x21, [sp, #0xA0]
str x30, [sp, #0xB8]
loadctxptr
mov w0, #1
str w0, [x19, #0x31C]
mov x0, #0
movk x0, #0, lsl #16
movk x0, #0, lsl #32
movk x0, #0, lsl #48
blr x0
str wzr, [x19, #0x31C]
str x0, [sp, #0xB0]
ldr x30, [sp, #0xB8]
ldp x20, x21, [sp, #0xA0]
ldp x18, x19, [sp, #0x90]
ldp x16, x17, [sp, #0x80]
ldp x14, x15, [sp, #0x70]
ldp x12, x13, [sp, #0x60]
ldp x10, x11, [sp, #0x50]
ldp x8, x9, [sp, #0x40]
ldp x6, x7, [sp, #0x30]
ldp x4, x5, [sp, #0x20]
ldp x2, x3, [sp, #0x10]
ldp x0, x1, [sp], #0xB0
ldr x0, [sp], #0x10
b #0
