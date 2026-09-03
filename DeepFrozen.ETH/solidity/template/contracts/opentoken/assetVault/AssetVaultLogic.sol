// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./AssetVaultLayout.sol";
import "./AssetVaultLogic1.sol";
import "./AssetVaultLogic2.sol";
import "./AssetVaultLogic3.sol";
import "./AssetVaultLogic4.sol";
import "./AssetVaultInterface.sol";

import "./AssetVaultType.sol";

//you can use this 4 in 1
//or use AssetVaultLogic1, AssetVaultLogic2, AssetVaultLogic3, AssetVaultLogic4 separately
abstract contract AssetVaultLogic is AssetVaultLayout, AssetVaultLogic1, AssetVaultLogic2, AssetVaultLogic3, AssetVaultLogic4, AssetVaultInterface {

    using SafeMath for uint256;
    using SafeERC20 for IERC20;


}
