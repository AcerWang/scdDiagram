/*
 Navicat Premium Data Transfer

 Source Server         : test
 Source Server Type    : SQLite
 Source Server Version : 3021000
 Source Schema         : main

 Target Server Type    : SQLite
 Target Server Version : 3021000
 File Encoding         : 65001

 Date: 05/07/2018 21:58:50
*/

PRAGMA foreign_keys = false;

-- ----------------------------
-- Table structure for AccessPoint
-- ----------------------------
DROP TABLE IF EXISTS "AccessPoint";
CREATE TABLE "AccessPoint" (
  "id" INT NOT NULL,
  "name" varchar(10),
  "router" varchar(10),
  "clock" varchar(10),
  PRIMARY KEY ("id")
);

-- ----------------------------
-- Table structure for DAI
-- ----------------------------
DROP TABLE IF EXISTS "DAI";
CREATE TABLE "DAI" (
  "id" INT NOT NULL,
  "doi_id" INT,
  "name" varchar(20),
  "val" varchar(40),
  PRIMARY KEY ("id")
);

-- ----------------------------
-- Table structure for DOI
-- ----------------------------
DROP TABLE IF EXISTS "DOI";
CREATE TABLE "DOI" (
  "id" INT NOT NULL,
  "ln_id" INT,
  "name" VARCHAR(20),
  "desc" VARCHAR(40)
);

-- ----------------------------
-- Table structure for DataSet
-- ----------------------------
DROP TABLE IF EXISTS "DataSet";
CREATE TABLE "DataSet" (
  "id" INT NOT NULL,
  "ln0_id" INT,
  "desc" varchar(40),
  "name" varchar(40),
  PRIMARY KEY ("id")
);

-- ----------------------------
-- Table structure for ExtRef
-- ----------------------------
DROP TABLE IF EXISTS "ExtRef";
CREATE TABLE "ExtRef" (
  "id" INT NOT NULL,
  "ln0_id" INT,
  "iedName" varchar(20),
  "prefix" varchar(20),
  "doName" varchar(20),
  "lnInst" varchar(10),
  "lnClass" varchar(20),
  "daName" varchar(20),
  "intAddr" varchar(40),
  "ldInst" varchar(20),
  PRIMARY KEY ("id")
);

-- ----------------------------
-- Table structure for FCDA
-- ----------------------------
DROP TABLE IF EXISTS "FCDA";
CREATE TABLE "FCDA" (
  "fcda" varchar(20) NOT NULL,
  "dataset" varchar(20),
  "ldInst" varchar(20),
  "prefix" varchar(20),
  "lnInst" varchar(20),
  "lnClass" varchar(20),
  "doName" varchar(20),
  "fc" varchar(20),
  PRIMARY KEY ("fcda")
);

-- ----------------------------
-- Table structure for GSEControl
-- ----------------------------
DROP TABLE IF EXISTS "GSEControl";
CREATE TABLE "GSEControl" (
  "gsecontrol" varchar(20) NOT NULL,
  "ln0" varchar(20),
  "appID" varchar(40),
  "datSet" varchar(20),
  "cnfRev" varchar(10),
  "name" varchar(20),
  "type" varchar(20),
  PRIMARY KEY ("gsecontrol")
);

-- ----------------------------
-- Table structure for IED
-- ----------------------------
DROP TABLE IF EXISTS "IED";
CREATE TABLE "IED" (
  "id" INT NOT NULL,
  "name" varchar(20),
  "type" varchar(20),
  "manufacturer" varchar(20),
  "desc" varchar(80),
  "configVersion" varchar(40),
  PRIMARY KEY ("id"),
  UNIQUE ("id" ASC)
);

-- ----------------------------
-- Table structure for IEDTree
-- ----------------------------
DROP TABLE IF EXISTS "IEDTree";
CREATE TABLE "IEDTree" (
  "IED" VARCHAR(20) NOT NULL,
  "Services_id" INT,
  "AccessPoint" VARCHAR(20),
  "AP_id" INT,
  "Server_id" INT,
  "LDevice" VARCHAR(20),
  "LD_id" INT
);

-- ----------------------------
-- Table structure for LDevice
-- ----------------------------
DROP TABLE IF EXISTS "LDevice";
CREATE TABLE "LDevice" (
  "id" INT NOT NULL,
  "inst" varchar(20),
  "desc" varchar(40),
  PRIMARY KEY ("id")
);

-- ----------------------------
-- Table structure for LN
-- ----------------------------
DROP TABLE IF EXISTS "LN";
CREATE TABLE "LN" (
  "id" INT NOT NULL,
  "ldevice_id" INT,
  "lnType" varchar(40),
  "lnClass" varchar(20),
  "prefix" varchar(20),
  "inst" varchar(10),
  "desc" varchar(40),
  PRIMARY KEY ("id")
);

-- ----------------------------
-- Table structure for LogControl
-- ----------------------------
DROP TABLE IF EXISTS "LogControl";
CREATE TABLE "LogControl" (
  "logcontrol" varchar(20) NOT NULL,
  "ln0" varchar(20),
  "name" varchar(40),
  "datSet" varchar(40),
  "intgPd" varchar(10),
  "logName" varchar(20),
  "logEna" varchar(10),
  "reasonCode" varchar(10),
  "desc" varchar(40),
  PRIMARY KEY ("logcontrol")
);

-- ----------------------------
-- Table structure for Server
-- ----------------------------
DROP TABLE IF EXISTS "Server";
CREATE TABLE "Server" (
  "id" INT,
  "timeout" VARCHAR(10)
);

-- ----------------------------
-- Table structure for Services
-- ----------------------------
DROP TABLE IF EXISTS "Services";
CREATE TABLE "Services" (
  "id" INT
);

-- ----------------------------
-- Table structure for tmp
-- ----------------------------
DROP TABLE IF EXISTS "tmp";
CREATE TABLE "tmp" (
  "Line" TEXT,
  "Ref_To" TEXT,
  "lnClass" TEXT,
  "lnInst" TEXT,
  "ldInst" TEXT,
  "prefix" TEXT,
  "doName" TEXT
);

PRAGMA foreign_keys = true;
