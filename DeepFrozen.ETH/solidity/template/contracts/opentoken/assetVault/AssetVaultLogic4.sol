// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./AssetVaultLayout.sol";
import "../../nameserver/nameServiceRef/NameServiceRefLogic.sol";
import "../holders/HERC721HolderLogic.sol";
import "../holders/HERC1155HolderLogic.sol";
import "./AssetVaultInterface.sol";

import "../erc20improved/HERC20IMInterface.sol";
import "../erc1155improved/HERC1155IMInterface.sol";
import "../erc721improved/HERC721IMInterface.sol";

import "./AssetVaultType.sol";

abstract contract AssetVaultLogic4 is AssetVaultLayout, NameServiceRefLogic, AssetVaultInterface4 {

    using SafeMath for uint256;
    using SafeERC20 for IERC20;

    function _setErc20Strategy(bool[] memory withdraw, bytes32[] memory erc20TokenName, uint256[] memory strategy, address[] memory strategyAddress) internal {

        for (uint256 i = 0; i < withdraw.length; i++) {
            ns().getSingleSafe(erc20TokenName[i]);

            if (!withdraw[i]) {
                require(AssetVaultType.ERC20_DEPOSIT_STRATEGY_UNKNOWN < strategy[i] && strategy[i] <= AssetVaultType.ERC20_DEPOSIT_STRATEGY_STORE, "deposit strategy invalid");
                _erc20DepositStrategy[erc20TokenName[i]] = strategy[i];
                _erc20DepositStrategyAddress[erc20TokenName[i]] = strategyAddress[i];
            } else {
                require(AssetVaultType.ERC20_WITHDRAW_STRATEGY_UNKNOWN < strategy[i] && strategy[i] <= AssetVaultType.ERC20_WITHDRAW_STRATEGY_RESTORE_TRANSFER, "withdraw strategy invalid");
                _erc20WithdrawStrategy[erc20TokenName[i]] = strategy[i];
                _erc20WithdrawStrategyAddress[erc20TokenName[i]] = strategyAddress[i];
            }
        }
    }

    function _setErc1155Strategy(bool[] memory withdraw, bytes32[] memory erc1155TokenName, uint256[] memory strategy, address[] memory strategyAddress) internal {

        for (uint256 i = 0; i < withdraw.length; i++) {
            ns().getSingleSafe(erc1155TokenName[i]);

            if (!withdraw[i]) {
                require(AssetVaultType.ERC1155_DEPOSIT_STRATEGY_UNKNOWN < strategy[i] && strategy[i] <= AssetVaultType.ERC1155_DEPOSIT_STRATEGY_STORE, "deposit strategy invalid");
                _erc1155DepositStrategy[erc1155TokenName[i]] = strategy[i];
                _erc1155DepositStrategyAddress[erc1155TokenName[i]] = strategyAddress[i];
            } else {
                require(AssetVaultType.ERC1155_WITHDRAW_STRATEGY_UNKNOWN < strategy[i] && strategy[i] <= AssetVaultType.ERC1155_WITHDRAW_STRATEGY_RESTORE_TRANSFER, "withdraw strategy invalid");
                _erc1155WithdrawStrategy[erc1155TokenName[i]] = strategy[i];
                _erc1155WithdrawStrategyAddress[erc1155TokenName[i]] = strategyAddress[i];
            }
        }
    }

    function _getErc20Strategy(bool withdraw, bytes32 erc20TokenName) view internal returns (uint256 strategy){

        ns().getSingleSafe(erc20TokenName);

        if (!withdraw) {
            return _erc20DepositStrategy[erc20TokenName];
        } else {
            return _erc20WithdrawStrategy[erc20TokenName];
        }
    }

    function _getErc20StrategyAddress(bool withdraw, bytes32 erc20TokenName) view internal returns (address strategyAddress){

        ns().getSingleSafe(erc20TokenName);

        if (!withdraw) {
            return _erc20DepositStrategyAddress[erc20TokenName];
        } else {
            return _erc20WithdrawStrategyAddress[erc20TokenName];
        }
    }

    function _getErc1155Strategy(bool withdraw, bytes32 erc1155TokenName) view internal returns (uint256 strategy){

        ns().getSingleSafe(erc1155TokenName);

        if (!withdraw) {
            return _erc1155DepositStrategy[erc1155TokenName];
        } else {
            return _erc1155WithdrawStrategy[erc1155TokenName];
        }
    }

    function _getErc1155StrategyAddress(bool withdraw, bytes32 erc1155TokenName) view internal returns (address strategyAddress) {

        ns().getSingleSafe(erc1155TokenName);

        if (!withdraw) {
            return _erc1155DepositStrategyAddress[erc1155TokenName];
        } else {
            return _erc1155WithdrawStrategyAddress[erc1155TokenName];
        }
    }
}
