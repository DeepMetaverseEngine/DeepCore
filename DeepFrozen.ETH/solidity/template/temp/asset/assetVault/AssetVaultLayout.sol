// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../preclude/Preclude.sol";

import "../nameServiceRef/NameServiceRefLayout.sol";

abstract contract AssetVaultLayout is NameServiceRefLayout {

    //erc20Token => strategy
    mapping(address => uint256) internal _erc20DepositStrategy;
    mapping(address => address) internal _erc20DepositStrategyAddress;
    mapping(address => uint256) internal _erc20WithdrawStrategy;
    mapping(address => address) internal _erc20WithdrawStrategyAddress;

    //erc1155Token => strategy
    mapping(address => uint256) internal _erc1155DepositStrategy;
    mapping(address => address) internal _erc1155DepositStrategyAddress;
    mapping(address => uint256) internal _erc1155WithdrawStrategy;
    mapping(address => address) internal _erc1155WithdrawStrategyAddress;
}
