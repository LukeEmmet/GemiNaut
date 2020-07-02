REBOL [
    title: "some useful gopher specific utility functions and values"
]


do load %utils.r3

gopher-escape-prefix: " ::¬5::¬ "    ;prefix used to escape all text content to prevent futher interpretaion by GMI converter - should be v infrequently used as a prefix

gopher-escape: funct [input] [
    rejoin [gopher-escape-prefix  input]
]

gopher-unescape: funct [input] [

        data: copy input
        escape-length: length? gopher-escape-prefix
        ;unescape extra spaces put there by gopher text pre-processor
        if escape-length <=  length? data [
            if gopher-escape-prefix = take-left data escape-length [
                data: take-from data (1 + escape-length)
            ]
        ]
        
        :data
]

extract-url: funct [gopher-field] [
    
    ;both seen in the wild, second and third forms are more common though
    any [
        if ("/URL:" = take-left gopher-field 5) [take-from gopher-field 6]
        if ("URL:" = take-left gopher-field 4) [take-from gopher-field 5]
        gopher-field
    ]
]

title-case: funct [text] [
   join (uppercase take-left text 1) (take-from text 2)

]


gopher-uri-to-title: funct [uri trim-extension] [
    ;a method to try to work out the nicest looking
    ;title based on the URI only. Use the last segment if not very short, without extension, as the basis
    ;otherwise use the domain
        
    internal-selector:  ["0" | "1" | "7" | "3"] 
    
    
    either ( parse uri [thru "gopher://" copy domain to "/" thru "/"  internal-selector copy path to end]) [
    
        replace/all  path "%09" "/"  ;so search queries pick up the query
        replace/all  path "%3F" "/"  ;so search queries pick up the query
        replace/all  path "?" "/"  ;so search queries pick up the query
        replace/all  path "/~" "/"  ;normalise user name in path to just pick out the name
        
        if ("/" = take-left path 1) [path: take-from path 2]
        
        either 1 < length? path [
            last-segment: last (parse/all path "/")
            
            new-title: copy last-segment
            
            ;remove txt extensions only - others may be informative
            if trim-extension and (1 <  length? parse/all new-title ".") [
                if "txt" = last parse/all new-title "." [
                    new-title: block-join  (head remove-last parse new-title ".") "."
                ]
            ]
        
            ;if title is of length 3 or less, use the whole path
            either 4 > length? new-title [            
                new-title: path
                
                ;remove txt extensions only - others may be informative
                if trim-extension and (1 <  length? parse/all new-title ".") [
                    if "txt" = last parse/all new-title "." [
                        new-title: block-join  (head remove-last parse new-title ".") "."
                    ]
                ]
                
                new-title: reform parse/all new-title ":/_-."    
                
            ]  [
                new-title: reform parse/all new-title " -_:"
            ]
            
            ;--if title still v short (short path, no file name) just use the domain
            if 3 > length? new-title [new-title: domain]
        ] [
            new-title: domain
        ]
    ] [
        ;top level domain only
        if not parse uri [thru "gopher://" copy domain to "/" to end] [
            parse uri [thru "gopher://" copy domain to end]
        ]
        new-title: domain
    ]
    
    
    replace/all new-title "%20" " "
    title-case new-title
    
]

tests: [
    "gopher://circumlunar.space/1/~solderpunk/phlog"
    "gopher://circumlunar.space/"
    "gopher://gopher.floodgap.com/7/v2/vs%09test"
    "gopher://tilde.team/1/"
    "gopher://tilde.team"
    "gopher://aussies.space/1/~brendantcc/"
    "gopher://typed-hole.org/0/~julienxx/Log/lobste.rs.txt"
    "gopher://sdf.org/1/users/julienxx/Lobste.rs"
    ]

;uncomment to test
;foreach test tests [ print gopher-uri-to-title probe test true]