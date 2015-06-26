A Typist's Disassembler for the 65816 in Forth
Scot W. Stevenson <scot.stevenson@gmail.com>
First version: 23. June 2015
This version: 26. June 2015

This is a disassembler for the 65816 8/16-bit hybrid MPU written in Gforth. It is BETA software, which means that it is feature-complete, but not thoroughly tested. It is a companion to the Typist's Assembler (https://github.com/scotws/tasm65816) and the Crude Emulator for the 65816 (https://github.com/scotws/crude65816), both currently in development. It written in the computer language Forth ( https://en.wikipedia.org/wiki/Forth_(programming_language) ). 



WAIT, WHAT'S THIS ABOUT A TYPIST?

This collection of programs uses a different assembler syntax than usual for the 6502 or 65816. It is designed to be quick and easy to type with ten fingers -- almost everything is lower case, and there are hardly any special characters. This variant is made to work well with Forth, which is why it uses postfix notation ("ass-backwards"). For example, 

    LDA $10       becomes         10 lda.d    

The assembler code is actually Forth code, so we don't have to use things like the number formats because we can use HEX, BINARY, and DECIMAL. A complete list is: 

    MODE                      WDC SYNTAX       TYPIST'S SYNTAX

    implied                   DEX                    dex
    accumulator               INC                    inc.a
    immediate                 LDA #$00            00 lda.#
    absolute                  LDA $1000         1000 lda
    absolute x indexed        LDA $1000,X       1000 lda.x
    absolute y indexed        LDA $1000,Y       1000 lda.y
    absolute indirect         JMP ($1000)       1000 jmp.i
    indexed indirect          JMP ($1000,X)     1000 jmp.xi
    absolute long             JMP $101000     101000 jmp.l    (65816)
    absolute long x indexed   JMP $101000,X   101000 jmp.lx   (65816)
    absolute indirect long    JMP [$1000]       1000 jmp.il   (65816)
    direct page               LDA $10             10 lda.d
    direct page x indexed     LDA $10,X           10 lda.dx
    direct page y indexed     LDA $10,Y           10 lda.dy
    direct page indirect      LDA ($10)           10 lda.di
    dp indirect x indexed     LDA ($10,X)         10 lda.dxi
    dp indirect long          LDA [$10]           10 lda.dil  (65816) 
    dp indirect y indexed     LDA ($10),Y         10 lda.diy  
    dp indirect long y index  LDA [$10],Y         10 lda.dily (65816)
    relative                  BNE $2F00         2f00 bne
    relative long             BRL $20F000     20f000 bra.l    (65816)
    stack relative            LDA 3,S              3 lda.s    (65816)
    stack rel ind y indexed   LDA (3,S),Y          3 lda.siy  (65816)
    block move                MVP 0,0            0 0 mvp      (65816) 

Note that the "i" (for "indirect") is put where the parenthesis is in normal notation (compare lda.zxi and lda.ziy). More on the assembler format and other background is contained in the MANUAL.txt with the Typist's Assembler.



SO THIS IS THE STUFF THE DISASSEMBLER PRODUCES?

Yes. You take a binary file with 65816 machine code -- say, "rom.bin" -- and translate it back to Typist's assembler. You do this by starting gforth, and first loading the disassembler with the command

        include tdis65816.fs

Next, you load the binary file into memory:

        s" rom.bin" loadbinary

This puts the beginning address and the length of the file on the stack, "( addr u )" in the normal Forth nation. To disassemble, type 

        disassemble

and the file will be printed to the screen. 



FEATURES 

The disassembler has a few more features than just putting out the assembler code:

- Tries its best to track the 8/16-bit register changes and change the size of the registers and accumulator accordingly

- Calculates targets of branches as lines for easier reading

- Inserts a blank line after stp, rti, rts, rts.l and a few other instructions

- Warns if the wdm instruction is encountered

It still remains a primitive tool.



LEGAL STUFF

A Typist's 65816 Disassembler in Forth is released under the GNU Public License, see LICENSE. Use this software at your own risk. 


LINKS 

- For all things to do with the 6502/65c02/65816, see http://www.6502.org/
Very nice, very helpful people. 

- Brad Rodriguez has published a series of articles on how to write your own assembler in Forth which were invaluable for this project. See those and other writings on Forth at http://www.bradrodriguez.com/papers/

- Backgrounder book: "Assemblers And Loaders" by David Salomon (1993). Available as free PDF from http://www.davidsalomon.name/assem.advertis/AssemAd.html
