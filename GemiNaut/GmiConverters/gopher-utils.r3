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


