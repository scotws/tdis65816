\ A Typist's Disassembler for the 65816 in Forth 
\ Copyright 2015 Scot W. Stevenson <scot.stevenson@gmail.com>
\ Written with gforth 0.7
\ First version: 23. June 2015
\ This version: 26. June 2015

\ This program is free software: you can redistribute it and/or modify
\ it under the terms of the GNU General Public License as published by
\ the Free Software Foundation, either version 3 of the License, or
\ (at your option) any later version.

\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.

\ You should have received a copy of the GNU General Public License
\ along with this program.  If not, see <http://www.gnu.org/licenses/>.

hex

variable start  0 start ! 
variable limit  

\ Routines for 8/16 hybrid functions

variable e-flag   \ true: emulation mode 
variable m-flag   \ true: A is 8 bit 
variable x-flag   \ true: XY are 8 bit 

: a:8  ( -- ) true m-flag ! ;   
: a:16  ( -- ) false m-flag ! ; 
: a8? ( -- f ) m-flag @ ;     

: xy:8  ( -- ) true x-flag ! ; 
: xy:16  ( -- ) false x-flag ! ; 
: xy8? ( -- f ) x-flag @ ;    

\ Load binary file into memory. Use s" FILENAME" to call
: loadbinary  ( addr u -- addr u )  r/o open-file  drop   slurp-fid ;       

\ Create printable numbers out of lsb/msb/bank combinations
: lsb/msb>16  ( lsb msb - u ) 8 lshift  0ff00 and  or  0ffff and ;
: lsb/msb/bank>24        ( lsb msb bank - u )
   -rot lsb/msb>16       ( bank u16 )
   swap                  ( u16 bank )
   0ff and  10 lshift    ( u16 u8 )
   or ;

\ String formating details, assumes HEX
: lower ( c -- c ) dup [char] A [char] Z 1+ within if 20 + then ;
: tolower ( addr u -- addr u ) 2dup  over + swap  do i c@ lower i c!  loop ;

\ Format output, assumes HEX
: .8bit ( u -- ) s>d <# # # #> tolower type space ;
: .16bit ( u -- ) s>d <# # # # # #> tolower type space ;
: .24bit ( u -- ) s>d <# # # # # # # #> tolower type space ;

\ Space between code and comments
: .commentspace  ( -- ) 7 spaces ; 

\ --- HANDLE INSTUCTION TYPES AND LENGTHS --- 
\ The main loop prints the counter and increases the address by one, and 
\ later prints the opcode string. We therefore need to print the invididual 
\ bytes of the instruction as they are in memory and the in postfix notation
\ before the opcode. 

\ Common element: Fetch one byte and print it, leaving a copy on the stack
: .onebyte ( addr -- addr+1 u ) 
   1+  dup c@  dup .8bit ; 

: 1byte ( -- )  12 spaces ;    

: 2byte ( addr -- addr+1 ) .onebyte  0c spaces  .8bit ;     

: 3byte ( addr -- addr+2 ) 
   .onebyte  swap  .onebyte  7 spaces  rot swap   lsb/msb>16  .16bit ;    

: 4byte ( addr -- addr+3 ) 
   .onebyte swap        ( lsb addr+1 ) 
   .onebyte swap        ( lsb msb addr+2 ) 
   .onebyte swap        ( lsb msb bank addr+3 ) 
   >r                   ( lsb msb bank ) 
   lsb/msb/bank>24  2 spaces  .24bit   
   r> ; 
 
\ handle short branches (BRANCH is reserved by Forth)
: twig8 ( addr -- addr+1 ) 
   .onebyte             ( addr+1 offset )
   0a spaces 
   over 1+  start @ -   ( addr+1 offset addr ) 
   over 80 and  if 
      100 swap - - else ( addr+1 addr ) 
      + then 
   .16bit ; 

\ handle long branches (BRANCH is reserved by Forth)
: twig16 ( addr -- addr+2 ) 
   .onebyte  swap  .onebyte  7 spaces 
   rot swap lsb/msb>16  ( addr+2 offset ) 
   over 1+  start @ -   ( addr+2 offset addr ) 
   over 8000 and  if 
      10000 swap - - else ( addr+2 addr ) 
      + then 
   .16bit ; 

\ handle block moves (MVP, MVN) Note reverse order of opcode and 
\ assembler (BLOCK and MOVE are reserved by Forth) 
: blkmov ( addr -- addr+2 ) 
   .onebyte  swap      ( dest addr+1 ) 
   .onebyte  6 spaces  ( dest addr+2 src ) 
   rot swap            ( addr+2 dest src ) 
   .8bit .8bit ; 
   
\ Handle the 8/16 bit hybrid instructions
: a8/16  ( addr -- addr+1 | addr+2 )  a8? if 2byte else 3byte then ; 
: xy8/16  ( addr -- addr+1 | addr+2 )  xy8? if 2byte else 3byte then ; 



\ --- SPECIAL CASES AND EXTRA MESSAGES ---

\ print comment for mode switch
: .xce ( addr -- addr ) 
   dup 1- c@      \ get previous opcode
   case  .commentspace
      18 of ." native" endof   \ clc
      38 of ." emulated" endof \ sec
      ." warning: no clc or sec before xce" endcase ; 

\ print comment for stp command
: .stp ( -- ) .commentspace  ." halt" cr ; 

\ print comment for wai command 
: .wai ( -- ) .commentspace  ." wait" cr ; 

\ print warning for wdm command
: .wdm ( -- ) .commentspace  ." warning: opcode reserved" ; 

\ handle register size switch and add comment
: .sep  ( addr+1 -- addr+1) 
   dup c@
   case  .commentspace
      10 of xy:8  ." xy:8" endof
      20 of a:8  ." a:8" endof
      30 of a:8 xy:8  ." axy:8" endof 
      ." warning: unusual use of sep" endcase ; 

 \ print comment for rep register size switch
: .rep  ( addr+1 -- addr+1) 
   dup c@
   case  .commentspace
      10 of xy:16  ." xy:16" endof
      20 of a:16  ." a:16" endof
      30 of a:16 xy:16  ." axy:16" endof 
      ." warning: unusual use of rep" endcase ; 
   

\ --- OPCODE LIST --- 

: opc-00  1byte ." brk" ;        : opc-01  2byte ." ora.dxi" ;  
: opc-02  2byte ." cop" ;        : opc-03  2byte ." ora.s" ; 
: opc-04  2byte ." tsb.d" ;      : opc-05  2byte ." ora.d" ; 
: opc-06  2byte ." asl.d" ;      : opc-07  2byte ." ora.dil" ; 
: opc-08  1byte ." php" ;        : opc-09  a8/16 ." ora.#" ;   
: opc-0A  1byte ." asl.a" ;      : opc-0B  1byte ." phd" ; 
: opc-0C  3byte ." tsb" ;        : opc-0D  3byte ." ora" ; 
: opc-0E  3byte ." asl" ;        : opc-0F  4byte ." ora.l" ; 

: opc-10  twig8 ." bpl" ;        : opc-11  2byte ." ora.diy" ; 
: opc-12  2byte ." ora.di" ;     : opc-13  2byte ." ora.siy" ;
: opc-14  2byte ." trb.d" ;      : opc-15  2byte ." ora.dx" ; 
: opc-16  2byte ." asl.dx" ;     : opc-17  2byte ." ora.dily" ;  
: opc-18  1byte ." clc" ;        : opc-19  3byte ." ora.y" ; 
: opc-1A  1byte ." inc.a" ;      : opc-1B  1byte ." tcs" ; 
: opc-1C  3byte ." trb" ;        : opc-1D  3byte ." ora.x" ;
: opc-1E  3byte ." asl.x" ;      : opc-1F  4byte ." ora.lx" ;

: opc-20  3byte ." jsr" ;        : opc-21  2byte ." and.dxi" ; 
: opc-22  4byte ." jsr.l" ;      : opc-23  2byte ." and.s" ; 
: opc-24  2byte ." bit.d" ;      : opc-25  2byte ." and.d" ; 
: opc-26  2byte ." rol.d" ;      : opc-27  2byte ." and.dil" ;  
: opc-28  1byte ." plp" ;        : opc-29  a8/16 ." and.#" ; 
: opc-2A  1byte ." rol.a" ;      : opc-2B  1byte ." pld" ;
: opc-2C  3byte ." bit" ;        : opc-2D  3byte ." and" ; 
: opc-2E  3byte ." rol" ;        : opc-2F  4byte ." and" ; 

: opc-30  twig8 ." bmi" ;        : opc-31  2byte ." and.diy" ; 
: opc-32  2byte ." and.di" ;     : opc-33  2byte ." and.siy" ; 
: opc-34  2byte ." bit.dx" ;     : opc-35  2byte ." and.dx" ; 
: opc-36  2byte ." rol.dx" ;     : opc-37  2byte ." and.dy" ; 
: opc-38  1byte ." sec" ;        : opc-39  3byte ." and.y" ;  
: opc-3A  1byte ." dec.a" ;      : opc-3B  1byte ." tsc" ; 
: opc-3C  3byte ." bit.x" ;      : opc-3D  3byte ." and.x" ; 
: opc-3E  3byte ." rol.x" ;      : opc-3F  4byte ." and.lx" ; 

: opc-40  1byte ." rti" cr ;     : opc-41  2byte ." eor.dxi" ;  
: opc-42  2byte ." wdm" .wdm ;   : opc-43  2byte ." eor.s" ;  
: opc-44  blkmov ." mvp" ;       : opc-45  2byte ." eor.d" ;      
: opc-46  2byte ." lsr.d" ;      : opc-47  2byte ." eor.dil" ;    
: opc-48  1byte ." pha" ;        : opc-49  a8/16 ." eor.#" ; 
: opc-4A  1byte ." lsr.a" ;      : opc-4B  1byte ." phk" ; 
: opc-4C  3byte ." jmp" ;        : opc-4D  3byte ." eor" ; 
: opc-4E  3byte ." lsr" ;        : opc-4F  4byte ." eor.l" ; 

: opc-50  twig8 ." bvc" ;        : opc-51  2byte ." eor.diy" ; 
: opc-52  2byte ." eor.di" ;     : opc-53  2byte ." eor.siy" ; 
: opc-54  blkmov ." mvn" ;       : opc-55  2byte ." eor.dx" ; 
: opc-56  2byte ." lsr.dx" ;     : opc-57  2byte ." eor.dy" ; 
: opc-58  1byte ." cli" ;        : opc-59  3byte ." eor.y" ; 
: opc-5A  1byte ." phy" ;        : opc-5B  1byte ." tcd" ; 
: opc-5C  4byte ." jmp.l" ;      : opc-5D  2byte ." eor.dx" ; 
: opc-5E  3byte ." lsr.x" ;      : opc-5F  4byte ." eor.lx" ; 

: opc-60  1byte ." rts" cr ;     : opc-61  2byte ." adc.dxi" ; 
: opc-62  twig16 ." phe.r" ;     : opc-63  2byte ." adc.s" ; 
: opc-64  2byte ." stz.d" ;      : opc-65  2byte ." adc.d" ;  
: opc-66  2byte ." ror.d" ;      : opc-67  2byte ." adc.dil" ; 
: opc-68  1byte ." pla" ;        : opc-69  a8/16 ." adc.#" ; 
: opc-6A  1byte ." ror.a" ;      : opc-6B  1byte ." rts.l" cr ; 
: opc-6C  3byte ." jmp.i" ;      : opc-6D  3byte ." adc" ;
: opc-6E  3byte ." ror" ;        : opc-6F  4byte ." adc.l" ; 

: opc-70  twig8 ." bvs" ;        : opc-71  2byte ." adc.diy" ; 
: opc-72  2byte ." adc.di" ;     : opc-73  2byte ." adc.siy" ; 
: opc-74  2byte ." stx.dx" ;     : opc-75  2byte ." adx.dx" ; 
: opc-76  2byte ." ror.dx" ;     : opc-77  2byte ." adc.dy" ; 
: opc-78  1byte ." sei" ;        : opc-79  3byte  ." ady.y" ; 
: opc-7A  1byte ." ply" ;        : opc-7B  1byte ." tdc" ; 
: opc-7C  3byte  ." jmp.xi" ;    : opc-7D  3byte  ." adc.x" ; 
: opc-7E  3byte  ." ror.x" ;     : opc-7F  4byte  ." adc.lx" ; 

: opc-80  twig8 ." bra" ;        : opc-81  2byte ." sta.dxi" ; 
: opc-82  twig16 ." bra.l" ;     : opc-83  2byte ." sta.s" ; 
: opc-84  2byte ." sty.d" ;      : opc-85  2byte ." sta.d" ; 
: opc-86  2byte ." stx.d" ;      : opc-87  2byte ." sta.dil" ; 
: opc-88  1byte ." dey" ;        : opc-89  a8/16 ." bit.#" ; 
: opc-8A  1byte ." txa" ;        : opc-8B  1byte ." phb" ; 
: opc-8C  3byte ." sty" ;        : opc-8D  3byte ." sta" ; 
: opc-8E  3byte ." stx" ;        : opc-8F  4byte ." sta.l" ; 

: opc-90  twig8 ." bcc" ;        : opc-91  2byte ." sta.diy" ; 
: opc-92  2byte ." sta.di" ;     : opc-93  2byte ." sta.siy" ;
: opc-94  2byte ." sty.dx" ;     : opc-95  2byte ." sta.dx" ;
: opc-96  2byte ." stx.dy" ;     : opc-97  2byte ." sta.dily" ;
: opc-98  1byte ." tya" ;        : opc-99  3byte ." sta.y" ; 
: opc-9A  1byte ." txs" ;        : opc-9B  1byte ." txy" ; 
: opc-9C  3byte ." stz" ;        : opc-9D  3byte ." sta.x" ;
: opc-9E  3byte ." stz.x" ;      : opc-9F  4byte ." sta.lx" ;

: opc-A0  xy8/16 ." ldy.#" ;     : opc-A1  2byte ." lda.dxi" ;
: opc-A2  xy8/16 ." ldx.#" ;     : opc-A3  2byte ." lda.s" ;
: opc-A4  2byte ." ldy.d" ;      : opc-A5  2byte ." lda.d" ; 
: opc-A6  2byte ." ldx.d" ;      : opc-A7  2byte ." lda.dil" ;
: opc-A8  1byte ." tay" ;        : opc-A9  a8/16 ." lda.#" ; 
: opc-AA  1byte ." tax" ;        : opc-AB  1byte ." plb" ; 
: opc-AC  3byte ." ldy" ;        : opc-AD  3byte ." lda" ; 
: opc-AE  3byte ." ldx" ;        : opc-AF  4byte ." lda.l" ; 

: opc-B0  twig8 ." bcs" ;        : opc-B1  2byte ." lda.diy" ; 
: opc-B2  2byte ." lda.di" ;     : opc-B3  2byte ." lda.siy" ; 
: opc-B4  2byte ." ldy.dx" ;     : opc-B5  2byte ." lda.dx" ; 
: opc-B6  2byte ." ldx.dy" ;     : opc-B7  2byte ." lda.dy" ; 
: opc-B8  1byte ." clv" ;        : opc-B9  3byte ." lda.y" ; 
: opc-BA  1byte ." tsx" ;        : opc-BB  1byte ." tyx" ;
: opc-BC  3byte ." ldy.x" ;      : opc-BD  3byte ." lda.x" ; 
: opc-BE  3byte ." ldx.y" ;      : opc-BF  4byte ." lda.lx" ; 

: opc-C0  xy8/16 ." cpy.#" ;     : opc-C1  2byte ." cpy.dxi" ;
: opc-C2  2byte ." rep" .rep ;   : opc-C3  2byte ." cmp.s" ;
: opc-C4  2byte ." cpy.d" ;      : opc-C5  2byte ." cmp.d" ;
: opc-C6  2byte ." dec.d" ;      : opc-C7  2byte ." cmp.dil" ;
: opc-C8  1byte ." iny" ;        : opc-C9  a8/16 ." cmp.#" ; 
: opc-CA  1byte ." dex" ;        : opc-CB  1byte ." wai" .wai ; 
: opc-CC  3byte ." cpy" ;        : opc-CD  3byte ." cmp" ;
: opc-CE  3byte ." dec" ;        : opc-CF  4byte ." cmp.l" ;

: opc-D0  twig8 ." bne" ;        : opc-D1  2byte ." cmp.diy" ;
: opc-D2  2byte ." cmp.di" ;     : opc-D3  2byte ." cmp.siy" ;
: opc-D4  2byte ." phe.di" ;     : opc-D5  2byte ." cmp.dx" ;
: opc-D6  2byte ." dec.dx" ;     : opc-D7  2byte ." cmp.dy" ;
: opc-D8  1byte ." cld" ;        : opc-D9  3byte ." cmp.y" ;
: opc-DA  1byte ." phx" ;        : opc-DB  1byte ." stp" .stp ; 
: opc-DC  3byte ." jmp.il" ;     : opc-DD  3byte ." cmp.x" ;
: opc-DE  3byte ." dec.x" ;      : opc-DF  4byte ." cmp.lx" ;

: opc-E0  xy8/16 ." cpx.#" ;     : opc-E1  2byte ." sbc.dxi" ;  
: opc-E2  2byte ." sep" .sep ;   : opc-E3  2byte ." sbc.s" ;
: opc-E4  2byte ." cpx.d" ;      : opc-E5  2byte ." sbc.d" ;
: opc-E6  2byte ." inc.d" ;      : opc-E7  2byte ." sbc.dil" ;
: opc-E8  1byte ." inx" ;        : opc-E9  a8/16 ." sbc.#" ; 
: opc-EA  1byte ." nop" ;        : opc-EB  1byte ." xba" ; 
: opc-EC  3byte ." cpx" ;        : opc-ED  3byte ." sbc" ;
: opc-EE  3byte ." inc" ;        : opc-EF  4byte ." sbc.l" ;

: opc-F0  twig8 ." beq" ;        : opc-F1  2byte ." sbc.diy" ; 
: opc-F2  2byte ." sbc.di" ;     : opc-F3  2byte ." sbc.siy" ; 
: opc-F4  3byte ." phe.#" ;      : opc-F5  2byte ." sbc.dx" ; 
: opc-F6  2byte ." inc.dx" ;     : opc-F7  2byte ." sbc.dily" ; 
: opc-F8  1byte ." sec" ;        : opc-F9  3byte ." sbc.y" ; 
: opc-FA  1byte ." plx" ;        : opc-FB  1byte ." xce" .xce ; 
: opc-FC  3byte ." jsr.xi" ;     : opc-FD  3byte ." sbc.x" ; 
: opc-FE  3byte ." inc.x" ;      : opc-FF  4byte ." sbc.lx" ; 


\ --- GENERATE OPCODE JUMP TABLE ---
\ Routine stores xt in table, offset is the opcode of the word in a cell. Use
\ "opc-jumptable <opcode> cells + @ execute" to call the opcode's word.
\ Assumes HEX

: make-opc-jumptable ( -- )
   100 0 do
      i s>d <# # # [char] - hold [char] c hold [char] p hold [char] o hold #>
      find-name name>int ,
   loop ;

create opc-jumptable   make-opc-jumptable 

: step ( u -- )  cells opc-jumptable +  @   execute ;

\ Print byte counter
: .counter  ( addr -- ) start @  -   .16bit  space ; 

\ --- MAIN ROUTINE ---

: disassemble  ( addr u -- )
   true e-flag !  a:8  xy:8   \ start in emulation mode, axy are 8 bit

   over start !     ( addr u ) 
   over +  limit !  ( addr ) 
   cr cr 

   begin          ( addr ) 
      dup .counter
      dup c@  dup .8bit  step   cr 
   1+ dup  limit @  >  until ; 

\ --- END ---
