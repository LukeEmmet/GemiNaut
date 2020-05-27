REBOL [
    title: "link building and parsing"

]

;===================================================
;    GemiNaut, a friendly browser for Gemini space on Windows
;    (and for related plucked instruments).
;
;    Copyright (C) 2020, Luke Emmet 
;
;    Email: luke [dot] emmet [at] gmail [dot] com
;
;    This program is free software: you can redistribute it and/or modify
;    it under the terms of the GNU General Public License as published by
;    the Free Software Foundation, either version 3 of the License, or
;    (at your option) any later version.
;
;    This program is distributed in the hope that it will be useful,
;    but WITHOUT ANY WARRANTY; without even the implied warranty of
;    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
;    GNU General Public License for more details.
;
;    You should have received a copy of the GNU General Public License
;    along with this program.  If not, see <https://www.gnu.org/licenses/>.
;===================================================

do load %utils.r3

get-site-id: func [uri-object] [

    site-id: uri-object/host

    path-parts:   parse/all uri-object/path "/"

    ;--try to guess the user path, if any by looking for "/users/foo" or "/~foo"
    keep-parts: copy []
    continue: true
    count: 1
    if (not none? find uri-object/path "/~") or (not none? find uri-object/path "/users/") [
       foreach path-part path-parts [
            if path-part = "users" [
                if not none? (pick path-parts count + 1) [
                    append keep-parts  path-part 
                    append keep-parts (pick path-parts count + 1)
                ]
                break
            ]
            
            if (substring  path-part 1 1) = "~" [
                append keep-parts  path-part 
                break
            ]

            append keep-parts  path-part 

            
            count: count + 1
        ]
        
        site-id: join uri-object/host ( block-join keep-parts "/")
    ]

    site-id

]

build-link: func [ uri-object link-part] [
                
        ;--need better scheme to build url, need to incorporate slashes ok
        ;--wrt uri
                
                
        uri-folder-parts: parse/all  uri-object/path "/"
        if (last uri-object/path) <> #"/" [
            remove-last uri-folder-parts      ;trim off last entry to get to containing folder
        ]

        ;probe uri-folder-parts
        new-path:    (block-join uri-folder-parts "/")
        if new-path = "" [new-path: "/"]
        if #"/" <> last new-path [append new-path "/"]

    
            ;--simplify links to current folder - we dont need leading ./
        if ("./" = substring link-part 1 2) [link-part: next next link-part]

                
        either (none? find  link-part "://") and (none? find link-part "maito:") [
            ;---protocol not given so we need to assemble a full path
            
            link:  any [
            
                ;check two slashes before just one next.
                if ("//" = substring link-part 1 2) [
                     rejoin [
                        (to-word  uri-object/scheme)
                        ":"
                        link-part
                    ]
                ]
                
                if ("/" = substring link-part 1 1) [
                     rejoin [
                        (to-word uri-object/scheme)
                        "://"
                        uri-object/host
                        link-part
                    ]
                ]
                
                ;default
                 rejoin  [
                        (to-word uri-object/scheme)
                        "://"
                        uri-object/host
                       
                        new-path
                        link-part
                    ]

                
            ]
            
        ] [
            ;--protocol is given so just return the whole thing
            link: link-part
        ]
                
    link
]

;print build-link (decode-url to-url "http://www.foo.com/users/") "bar"