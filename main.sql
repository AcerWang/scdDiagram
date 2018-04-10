/*
 Navicat Premium Data Transfer

 Source Server         : wxy
 Source Server Type    : SQLite
 Source Server Version : 3021000
 Source Schema         : main

 Target Server Type    : SQLite
 Target Server Version : 3021000
 File Encoding         : 65001

 Date: 10/04/2018 22:03:17
*/

PRAGMA foreign_keys = false;

-- ----------------------------
-- Table structure for AccessPoint
-- ----------------------------
DROP TABLE IF EXISTS "AccessPoint";
CREATE TABLE "AccessPoint" (
  "ap_id" INTEGER(10) NOT NULL AUTOINCREMENT,
  "ied_id" integer(10),
  "name" varchar(10),
  "ld_inst" varchar(20),
  "ld_desc" vachar(40),
  PRIMARY KEY ("ap_id")
);

-- ----------------------------
-- Table structure for DAI
-- ----------------------------
DROP TABLE IF EXISTS "DAI";
CREATE TABLE "DAI" (
  "dai_id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
  "doi_id" INTEGER(10),
  "name" varchar(20),
  "value" varchar(40)
);

-- ----------------------------
-- Table structure for DOI
-- ----------------------------
DROP TABLE IF EXISTS "DOI";
CREATE TABLE "DOI" (
  "doi_id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
  "ln_id" integer(10),
  "name" VARCHAR(20),
  "desc" VARCHAR(40)
);

-- ----------------------------
-- Table structure for DataSet
-- ----------------------------
DROP TABLE IF EXISTS "DataSet";
CREATE TABLE "DataSet" (
  "ds_id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
  "ln_id" INTEGER(10),
  "desc" varchar(40),
  "name" varchar(40)
);

-- ----------------------------
-- Table structure for FCDA
-- ----------------------------
DROP TABLE IF EXISTS "FCDA";
CREATE TABLE "FCDA" (
  "fcda_id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
  "ds_id" INTEGER(10),
  "ldInst" varchar(20),
  "prefix" varchar(20),
  "lnInst" varchar(20),
  "lnClass" varchar(20),
  "doName" varchar(20),
  "fc" varchar(20)
);

-- ----------------------------
-- Table structure for GSEControl
-- ----------------------------
DROP TABLE IF EXISTS "GSEControl";
CREATE TABLE "GSEControl" (
  "gse_id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
  "ln_id" INTEGER(10),
  "appID" varchar(40),
  "datSet" varchar(20),
  "cnfRev" varchar(10),
  "name" varchar(20),
  "type" varchar(20)
);

-- ----------------------------
-- Table structure for IED
-- ----------------------------
DROP TABLE IF EXISTS "IED";
CREATE TABLE "IED" (
  "ied_id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
  "name" varchar(20),
  "type" varchar(20),
  "manufactuer" varchar(20),
  "desc" varchar(80),
  "configVersion" varchar(40)
);

-- ----------------------------
-- Table structure for Inputs
-- ----------------------------
DROP TABLE IF EXISTS "Inputs";
CREATE TABLE "Inputs" (
  "ext_id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
  "ln_id" INTEGER(10),
  "iedName" varchar(20),
  "prefix" varchar(20),
  "doName" varchar(20),
  "lnInst" varchar(10),
  "lnClass" varchar(20),
  "daName" varchar(20),
  "intAddr" varchar(40),
  "ldInst" varchar(20)
);

-- ----------------------------
-- Table structure for LDevice
-- ----------------------------
DROP TABLE IF EXISTS "LDevice";
CREATE TABLE "LDevice" (
  "ld_id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
  "ap_id" INTEGER(10),
  "ld_inst" varchar(20),
  "desc" varchar(40)
);

-- ----------------------------
-- Table structure for LNode
-- ----------------------------
DROP TABLE IF EXISTS "LNode";
CREATE TABLE "LNode" (
  "ln_id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
  "ld_id" INTEGER,
  "lnType" varchar(40),
  "lnClass" varchar(20),
  "prefix" varchar(20),
  "inst" varchar(10),
  "desc" varchar(40)
);

-- ----------------------------
-- Table structure for LogControl
-- ----------------------------
DROP TABLE IF EXISTS "LogControl";
CREATE TABLE "LogControl" (
  "lc_id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
  "ln_id" INTEGER(10),
  "name" varchar(40),
  "datSet" varchar(40),
  "intgPd" varchar(10),
  "logName" varchar(20),
  "logEna" varchar(10),
  "reasonCode" varchar(10),
  "desc" varchar(40)
);

-- ----------------------------
-- Table structure for ReportControl
-- ----------------------------
DROP TABLE IF EXISTS "ReportControl";
CREATE TABLE "ReportControl" (
  "rc_id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
  "ln_id" INTEGER(10),
  "name" varchar(40),
  "datSet" varchar(40),
  "intgPd" varchar(10),
  "tptID" varchar(40),
  "confRev" varchar(10),
  "buffered" varchar(10),
  "bufTime" varchar(10),
  "desc" varchar(40)
);

-- ----------------------------
-- Auto increment value for DataSet
-- ----------------------------

-- ----------------------------
-- Auto increment value for FCDA
-- ----------------------------

-- ----------------------------
-- Auto increment value for IED
-- ----------------------------

-- ----------------------------
-- Auto increment value for Inputs
-- ----------------------------

-- ----------------------------
-- Auto increment value for LDevice
-- ----------------------------

-- ----------------------------
-- Auto increment value for LogControl
-- ----------------------------

PRAGMA foreign_keys = true;
