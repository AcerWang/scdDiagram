package main

import (
	"fmt"

	"encoding/xml"
	"io/ioutil"
	"os"
	"time"
)

type IED struct{
	Manufaturer   string   `xml:"manufaturer,attr"`
	Type          string   `xml:"type,attr"`
	ConfigVersion string   `xml:"configVersion,attr"`
	Name          string   `xml:"name,attr"`
	Desc          string   `xml:"desc,attr"`
}
type SCL struct {
	XMLName       xml.Name `xml:"SCL"`
	IED          []IED

}

func main() {
	start := time.Now()
	content, err := ioutil.ReadFile("HS220.scd")

	if err != nil {
		fmt.Printf("error: %v", err)
		os.Exit(1)

	}

	v := SCL{}
	err2 := xml.Unmarshal([]byte(content), &v)
	if err2 != nil {
		fmt.Printf("error: %v", err2)
		os.Exit(1)
	}
	//fmt.Printf("XMLName: %#v\n", v.XMLName)
	for _,item := range v.IED{
		fmt.Println(item)
	}
	dur := time.Since(start)

	fmt.Println(dur)
}
