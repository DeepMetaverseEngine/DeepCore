// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../../nameserver/preclude/Preclude.sol";

import "../../nameserver/nameServiceRef/NameServiceRefInterface.sol";

import "./AssetVaultEvent.sol";

interface AssetVaultInterface4 is NameServiceRefInterface, AssetVaultEvent {

    //if no address needed, pass address(0) to it
    function setErc20Strategy(bool[] memory withdraw, bytes32[] memory erc20TokenName, uint256[] memory strategy, address[] memory strategyAddress) external;

    function setErc1155Strategy(bool[] memory withdraw, bytes32[] memory erc1155TokenName, uint256[] memory strategy, address[] memory strategyAddress) external;

}
