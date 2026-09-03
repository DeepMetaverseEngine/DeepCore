// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;


import "./AssetVaultInterface1.sol";
import "./AssetVaultInterface2.sol";
import "./AssetVaultInterface3.sol";
import "./AssetVaultInterface4.sol";

interface AssetVaultInterface is
AssetVaultInterface1,
AssetVaultInterface2,
AssetVaultInterface3,
AssetVaultInterface4
{}
