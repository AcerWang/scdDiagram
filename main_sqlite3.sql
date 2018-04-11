/*
 Navicat Premium Data Transfer

 Source Server         : wxy
 Source Server Type    : SQLite
 Source Server Version : 3021000
 Source Schema         : main

 Target Server Type    : SQLite
 Target Server Version : 3021000
 File Encoding         : 65001

 Date: 11/04/2018 22:20:59
*/

PRAGMA foreign_keys = false;

-- ----------------------------
-- Table structure for AccessPoint
-- ----------------------------
DROP TABLE IF EXISTS "AccessPoint";
CREATE TABLE "AccessPoint" (
  "ap_id" varchar(20) NOT NULL,
  "ied_id" varchar(20),
  "name" varchar(10),
  "router" varchar(10),
  "clock" varchar(10),
  PRIMARY KEY ("ap_id")
);

-- ----------------------------
-- Table structure for DAI
-- ----------------------------
DROP TABLE IF EXISTS "DAI";
CREATE TABLE "DAI" (
  "dai_id" varchar(20) NOT NULL,
  "doi_id" varchar(20),
  "name" varchar(20),
  "value" varchar(40),
  PRIMARY KEY ("dai_id")
);

-- ----------------------------
-- Table structure for DOI
-- ----------------------------
DROP TABLE IF EXISTS "DOI";
CREATE TABLE "DOI" (
  "doi_id" varchar(20) NOT NULL,
  "ln_id" varchar(20),
  "name" VARCHAR(20),
  "desc" VARCHAR(40),
  PRIMARY KEY ("doi_id")
);

-- ----------------------------
-- Table structure for DataSet
-- ----------------------------
DROP TABLE IF EXISTS "DataSet";
CREATE TABLE "DataSet" (
  "ds_id" varchar NOT NULL,
  "ln0_id" varchar(20),
  "desc" varchar(40),
  "name" varchar(40),
  PRIMARY KEY ("ds_id")
);

-- ----------------------------
-- Table structure for FCDA
-- ----------------------------
DROP TABLE IF EXISTS "FCDA";
CREATE TABLE "FCDA" (
  "fcda_id" varchar(20) NOT NULL,
  "ds_id" varchar(20),
  "ldInst" varchar(20),
  "prefix" varchar(20),
  "lnInst" varchar(20),
  "lnClass" varchar(20),
  "doName" varchar(20),
  "fc" varchar(20),
  PRIMARY KEY ("fcda_id")
);

-- ----------------------------
-- Table structure for GSEControl
-- ----------------------------
DROP TABLE IF EXISTS "GSEControl";
CREATE TABLE "GSEControl" (
  "gse_id" varchar(20) NOT NULL,
  "ln_id" varchar(20),
  "appID" varchar(40),
  "datSet" varchar(20),
  "cnfRev" varchar(10),
  "name" varchar(20),
  "type" varchar(20),
  PRIMARY KEY ("gse_id")
);

-- ----------------------------
-- Table structure for IED
-- ----------------------------
DROP TABLE IF EXISTS "IED";
CREATE TABLE "IED" (
  "ied_id" varchar(20) NOT NULL,
  "name" varchar(20),
  "type" varchar(20),
  "manufactuer" varchar(20),
  "desc" varchar(80),
  "configVersion" varchar(40),
  PRIMARY KEY ("ied_id")
);

-- ----------------------------
-- Table structure for Inputs
-- ----------------------------
DROP TABLE IF EXISTS "Inputs";
CREATE TABLE "Inputs" (
  "ext_id" varchar(20) NOT NULL,
  "ln_id" varchar(20),
  "iedName" varchar(20),
  "prefix" varchar(20),
  "doName" varchar(20),
  "lnInst" varchar(10),
  "lnClass" varchar(20),
  "daName" varchar(20),
  "intAddr" varchar(40),
  "ldInst" varchar(20),
  PRIMARY KEY ("ext_id")
);

-- ----------------------------
-- Table structure for LDevice
-- ----------------------------
DROP TABLE IF EXISTS "LDevice";
CREATE TABLE "LDevice" (
  "ld_id" varchar(20) NOT NULL,
  "ap_id" varchar(20),
  "inst" varchar(20),
  "desc" varchar(40),
  PRIMARY KEY ("ld_id")
);

-- ----------------------------
-- Table structure for LN0
-- ----------------------------
DROP TABLE IF EXISTS "LN0";
CREATE TABLE "LN0" (
  "ln0_id" varchar(20) NOT NULL,
  "ld_id" varchar(20),
  "lnType" VARCHAR(40),
  "lnClass" varchar(20),
  "inst" varchar(10),
  PRIMARY KEY ("ln0_id")
);

-- ----------------------------
-- Table structure for LNode
-- ----------------------------
DROP TABLE IF EXISTS "LNode";
CREATE TABLE "LNode" (
  "ln_id" varchar(20) NOT NULL,
  "ld_id" varchar(20),
  "lnType" varchar(40),
  "lnClass" varchar(20),
  "prefix" varchar(20),
  "inst" varchar(10),
  "desc" varchar(40),
  PRIMARY KEY ("ln_id")
);

-- ----------------------------
-- Table structure for LogControl
-- ----------------------------
DROP TABLE IF EXISTS "LogControl";
CREATE TABLE "LogControl" (
  "lc_id" varchar(20) NOT NULL,
  "ln_id" varchar(20),
  "name" varchar(40),
  "datSet" varchar(40),
  "intgPd" varchar(10),
  "logName" varchar(20),
  "logEna" varchar(10),
  "reasonCode" varchar(10),
  "desc" varchar(40),
  PRIMARY KEY ("lc_id")
);

-- ----------------------------
-- Table structure for ReportControl
-- ----------------------------
DROP TABLE IF EXISTS "ReportControl";
CREATE TABLE "ReportControl" (
  "rc_id" varchar(20) NOT NULL,
  "ln_id" varchar(20),
  "name" varchar(40),
  "datSet" varchar(40),
  "intgPd" varchar(10),
  "tptID" varchar(40),
  "confRev" varchar(10),
  "buffered" varchar(10),
  "bufTime" varchar(10),
  "desc" varchar(40),
  PRIMARY KEY ("rc_id")
);

-- ----------------------------
-- Table structure for sqlite_sequence
-- ----------------------------
DROP TABLE IF EXISTS "sqlite_sequence";
CREATE TABLE "sqlite_sequence" (
  "name",
  "seq"
);

PRAGMA foreign_keys = true;
